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
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = $"你是星露谷物语游戏助手。以下是玩家当前的游戏数据(JSON格式):\n\n{gameContext}\n\n玩家问题: {question}\n\n请基于以上数据用简洁的中文回答。"
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
                string answer = contentArray[0].GetProperty("text").GetString() ?? "(空回复)";
                return answer;
            }
            catch (Exception ex)
            {
                return $"[请求失败]: {ex.Message}";
            }
        }
    }
}