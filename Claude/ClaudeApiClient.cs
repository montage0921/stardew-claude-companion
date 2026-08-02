using System;
using System.Collections.Generic;
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

        // messages 是完整的对话历史，包含最新这轮的问题(带游戏数据的完整prompt)。
        // 调用方负责拼装每条消息的具体内容，这里只负责发请求——这样调用方存下来的
        // "历史"和实际发给API的内容能保证完全一致，不会出现历史记录和真实prompt对不上的情况。
        public async Task<string> AskAsync(IReadOnlyList<ChatTurn> messages)
        {
            var messageObjects = messages.Select(turn => new { role = turn.Role, content = turn.Content }).ToList();

            var requestBody = new
            {
                model = "claude-sonnet-5",
                max_tokens = 1024,
                thinking = new { type = "disabled" },
                // Sonnet 5 支持带动态过滤的新版 web_search，搜索结果处理更精准。
                // 只在问题看起来需要时效性信息时才会真正触发搜索，多数游戏内数据问答不会用到，不额外耗时耗token。
                tools = new object[]
                {
                    new { type = "web_search_20260209", name = "web_search", max_uses = 3 }
                },
                messages = messageObjects
            };

            string jsonBody = JsonSerializer.Serialize(requestBody);

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
            request.Headers.Add("x-api-key", this.apiKey);
            request.Headers.Add("anthropic-version", "2023-06-01");
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

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