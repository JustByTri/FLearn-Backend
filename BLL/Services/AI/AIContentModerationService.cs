using Azure;
using Azure.AI.OpenAI;
using BLL.IServices.AI;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace BLL.Services.AI
{
    public class AIContentModerationService : IAIContentModerationService
    {
        private readonly IConfiguration _configuration;

        private const string MODERATION_SYSTEM_PROMPT = @"
            You are a strict automated content moderator. Your task is to analyze user reviews in ANY language (Vietnamese, English, Chinese, Japanese, etc.).
            
            You must flag the content as UNSAFE (is_safe: false) if it contains ANY of the following:
            1. Political Reactionary content (nội dung phản động, chống phá nhà nước/chính quyền, xuyên tạc lịch sử).
            2. Hate speech, Racism, or Ethnic discrimination (phân biệt chủng tộc, sắc tộc, vùng miền).
            3. Harassment, Humiliation, Insults, or Bullying (xúc phạm, lăng mạ, hạ nhục người khác).
            4. Violence, Physical threats, or Terrorist content (bạo lực, đe dọa).
            5. Sexual content, Nudity, or Inappropriate sexual suggestions (tình dục, đồi trụy).
            6. Spam or Scam content.

            If the content is purely constructive criticism, negative feedback about a course/teacher without insults, or normal conversation, flag it as SAFE (is_safe: true).

            IMPORTANT: Return ONLY valid JSON. No markdown formatting.
            Schema: { ""is_safe"": boolean, ""reason"": ""short_reason_string"" }";
        public AIContentModerationService(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public async Task<bool> IsContentSafeAsync(string textContent)
        {
            if (string.IsNullOrWhiteSpace(textContent)) return true;
            try
            {
                var result = await CheckWithAzureAsync(textContent);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Azure Moderation Failed]: {ex.Message}. Switching to Gemini...");
                try
                {
                    var result = await CheckWithGeminiAsync(textContent);
                    return result;
                }
                catch (Exception geminiEx)
                {
                    Console.WriteLine($"[Gemini Moderation Failed]: {geminiEx.Message}");
                    return true;
                }
            }
        }
        private async Task<bool> CheckWithAzureAsync(string text)
        {
            string deploymentName = _configuration["AzureOpenAISettings:ChatDeployment"];
            string endpoint = _configuration["AzureOpenAISettings:Endpoint"];
            string key = _configuration["AzureOpenAISettings:ApiKey"];

            AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
            var chatClient = openAIClient.GetChatClient(deploymentName);

            var chatMessages = new List<ChatMessage>
            {
                new SystemChatMessage(MODERATION_SYSTEM_PROMPT),
                new UserChatMessage(text)
            };

            ChatCompletionOptions options = new ChatCompletionOptions { MaxOutputTokenCount = 100, Temperature = 0 };

            ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages, options);

            if (chatCompletion == null || chatCompletion.Content.Count == 0)
                throw new Exception("Azure returned empty response.");

            return ParseAiResponse(chatCompletion.Content[0].Text);
        }
        private async Task<bool> CheckWithGeminiAsync(string text)
        {
            string[] apiKeys = new[]
            {
                "AIzaSyAsAA8w7QSEdW5k2CcbuWuEhXLV17hiYHI",
                "AIzaSyAtrnbsgiyDAQP1OCXpCbfXkroblGrryP0"
            };

            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new[] {
                            new { text = MODERATION_SYSTEM_PROMPT + "\n\nContent to check: " + text }
                        }
                    }
                },
                generationConfig = new
                {
                    response_mime_type = "application/json",
                    temperature = 0
                }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);

            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(10);

            foreach (var apiKey in apiKeys)
            {
                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
                    var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(responseString);
                        var candidates = doc.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            var resultText = candidates[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString();
                            return ParseAiResponse(resultText);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Gemini Key failed: {ex.Message}");
                    continue;
                }
            }

            throw new Exception("All Gemini keys failed.");
        }

        private bool ParseAiResponse(string jsonResponse)
        {
            try
            {
                var cleanJson = jsonResponse.Replace("```json", "").Replace("```", "").Trim();

                using var doc = JsonDocument.Parse(cleanJson);
                if (doc.RootElement.TryGetProperty("is_safe", out var isSafeElement))
                {
                    bool isSafe = isSafeElement.GetBoolean();
                    if (!isSafe)
                    {
                        string reason = doc.RootElement.TryGetProperty("reason", out var reasonEl) ? reasonEl.GetString() : "Unknown";
                        Console.WriteLine($"[CONTENT BLOCKED]: {reason}");
                    }
                    return isSafe;
                }
                return false; // Mặc định chặn nếu JSON không đúng format
            }
            catch
            {
                return false; // Chặn nếu lỗi parse
            }
        }
    }
}
