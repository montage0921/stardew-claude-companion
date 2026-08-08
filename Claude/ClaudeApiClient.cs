using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StardewClaudeCompanion
{
    public record ChatTurn(string Role, string Content);

    public class ClaudeApiClient
    {
        private readonly string apiKey;
        private static readonly HttpClient httpClient = new HttpClient();

        public ClaudeApiClient(string apiKey)
        {
            this.apiKey = apiKey;
        }

        // 组装请求体。stream=true 时 API 改用 SSE 逐块推送，其余参数两种模式完全一致。
        private string BuildRequestJson(IReadOnlyList<ChatTurn> messages, bool stream)
        {
            var messageObjects = messages.Select(turn => new { role = turn.Role, content = turn.Content }).ToList();

            var requestBody = new
            {
                model = "claude-sonnet-5",
                max_tokens = 1024,
                stream,
                thinking = new { type = "disabled" },
                // Sonnet 5 支持带动态过滤的新版 web_search，搜索结果处理更精准。
                // 只在问题看起来需要时效性信息时才会真正触发搜索，多数游戏内数据问答不会用到，不额外耗时耗token。
                tools = new object[]
                {
                    new { type = "web_search_20260209", name = "web_search", max_uses = 3 }
                },
                messages = messageObjects
            };

            return JsonSerializer.Serialize(requestBody);
        }

        private HttpRequestMessage BuildRequest(string jsonBody)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", this.apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");
            return request;
        }

        // 流式版本：每收到一小段文字就回调一次 onDelta，调用方可以边收边显示，
        // 消除"提问后长时间空白等待"的延迟感。返回值是拼接完成的完整回复，
        // 语义和 AskAsync 一致，方便调用方照旧存进对话历史。
        //
        // onDelta 是在 HTTP 读取线程上调用的，不是游戏主线程——调用方必须自己做线程同步。
        public async Task<string> AskStreamingAsync(IReadOnlyList<ChatTurn> messages, Action<string> onDelta)
        {
            string jsonBody = this.BuildRequestJson(messages, stream: true);
            using var request = this.BuildRequest(jsonBody);

            try
            {
                // HttpCompletionOption.ResponseHeadersRead 是关键：默认的 ResponseContentRead 会
                // 一直等到整个响应体下载完才返回，那样流式就完全失去意义了。
                using var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    string errorBody = await response.Content.ReadAsStringAsync();
                    return $"[API 错误 {response.StatusCode}]: {errorBody}";
                }

                using var stream = await response.Content.ReadAsStreamAsync();
                using var reader = new StreamReader(stream, Encoding.UTF8);

                var full = new StringBuilder();

                // SSE 格式：每个事件由若干 "字段: 值" 行组成，空行表示一个事件结束。
                // 这里只关心 data: 行，事件类型直接从 JSON 里的 "type" 字段读，
                // 不依赖 event: 行(两者内容一致，JSON 更可靠)。
                while (true)
                {
                    string? line = await reader.ReadLineAsync();
                    if (line == null) break; // 流正常结束

                    if (!line.StartsWith("data:", StringComparison.Ordinal))
                        continue;

                    string payload = line.Substring(5).Trim();
                    if (payload.Length == 0)
                        continue;

                    string? delta = ExtractTextDelta(payload);
                    if (delta != null && delta.Length > 0)
                    {
                        full.Append(delta);
                        onDelta(delta);
                    }
                }

                return full.Length > 0 ? full.ToString() : "(空回复)";
            }
            catch (Exception ex)
            {
                return $"[请求失败]: {ex.Message}";
            }
        }

        // 从单个 SSE data 负载里取出正文增量。
        // 只认 content_block_delta + text_delta 这一种组合：用了 web_search 之后流里还会混进
        // server_tool_use 的 input_json_delta(搜索关键词的 JSON 片段)，那些不是给玩家看的正文，
        // 必须按 delta.type 过滤掉，否则会把一堆 JSON 碎片画到对话框里。
        private static string? ExtractTextDelta(string payload)
        {
            try
            {
                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                if (!root.TryGetProperty("type", out var typeProp) || typeProp.GetString() != "content_block_delta")
                    return null;

                if (!root.TryGetProperty("delta", out var deltaProp))
                    return null;

                if (!deltaProp.TryGetProperty("type", out var deltaType) || deltaType.GetString() != "text_delta")
                    return null;

                return deltaProp.TryGetProperty("text", out var textProp) ? textProp.GetString() : null;
            }
            catch (JsonException)
            {
                // 心跳/注释之类的非 JSON 负载直接跳过，不影响后续事件。
                return null;
            }
        }

        // messages 是完整的对话历史，包含最新这轮的问题(带游戏数据的完整prompt)。
        // 调用方负责拼装每条消息的具体内容，这里只负责发请求——这样调用方存下来的
        // "历史"和实际发给API的内容能保证完全一致，不会出现历史记录和真实prompt对不上的情况。
        public async Task<string> AskAsync(IReadOnlyList<ChatTurn> messages)
        {
            string jsonBody = this.BuildRequestJson(messages, stream: false);
            using var request = this.BuildRequest(jsonBody);

            try
            {
                var response = await httpClient.SendAsync(request);
                string responseBody = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return $"[API 错误 {response.StatusCode}]: {responseBody}";
                }

                using var doc = JsonDocument.Parse(responseBody);
                var contentArray = doc.RootElement.GetProperty("content");

                // 用了 web_search 工具后，content 里除了 text 块，还可能夹着
                // server_tool_use / web_search_tool_result 这类块，只拼接文字块。
                var textParts = new System.Collections.Generic.List<string>();
                foreach (var block in contentArray.EnumerateArray())
                {
                    if (block.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "text"
                        && block.TryGetProperty("text", out var textProp))
                    {
                        textParts.Add(textProp.GetString() ?? "");
                    }
                }

                string answer = textParts.Count > 0 ? string.Join("\n", textParts) : "(空回复)";
                return answer;
            }
            catch (Exception ex)
            {
                return $"[请求失败]: {ex.Message}";
            }
        }
    }
}