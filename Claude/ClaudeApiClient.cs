using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace StardewClaudeCompanion
{
    public class ClaudeApiClient
    {
        private readonly string apiKey;
        private static readonly HttpClient httpClient = new HttpClient();

        public ClaudeApiClient(string apiKey)
        {
            this.apiKey = apiKey;
        }

        public async Task<string> AskAsync(string question, string gameContext)
        {
            var requestBody = new
            {
                model = "claude-haiku-4-5",
                max_tokens = 1024,
                // Haiku 4.5 不在 web_search_20260209(动态过滤版) 的支持列表里，用基础版 _20250305。
                // 只在问题看起来需要时效性信息时才会真正触发搜索，多数游戏内数据问答不会用到，不额外耗时耗token。
                tools = new object[]
                {
                    new { type = "web_search_20250305", name = "web_search", max_uses = 3 }
                },
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"你是星露谷物语游戏助手。以下是玩家当前的游戏数据(JSON格式):\n\n{gameContext}\n\n玩家问题: {question}\n\n请基于以上数据用简洁的中文回答。如果问题涉及游戏版本更新、最新内容等你不确定或可能过时的信息，可以使用联网搜索工具查证。回答会显示在游戏内的纯文本对话框里，不支持任何格式，所以不要使用Markdown语法（不要用**加粗**、#标题、-列表符号等），只用纯文字和换行组织内容。"
                    }
                }
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