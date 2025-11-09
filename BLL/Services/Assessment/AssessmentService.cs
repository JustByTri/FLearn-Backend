using Azure;
using Azure.AI.OpenAI;
using BLL.IServices.Assessment;
using BLL.Services.AI;
using Common.DTO.Assessment.Response;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace BLL.Services.Assessment
{
    public class AssessmentService : IAssessmentService
    {
        private readonly IConfiguration _configuration;
        private readonly ITranscriptionService _transcriptionService;
        private readonly IUnitOfWork _unitOfWork;
        private const string MULTILINGUAL_SCORING_SYSTEM_PROMPT = @"You are an expert speaking examiner for English, Japanese and Chinese.
                                                                    Always return EXACTLY valid JSON with the schema described.
                                                                    Do NOT output anything outside that JSON.
                                                                    Language codes: ""en"", ""ja"", ""zh"".

                                                                    Scoring fields (0-100 integers):
                                                                    pronunciation, fluency, coherence, accuracy, intonation, grammar, vocabulary
                                                                    Also: cefr_level (A1..C2), overall (0-100), feedback (string), transcript (string).

                                                                    Follow CEFR speaking descriptors. Use JLPT mapping for japanese and HSK mapping for chinese.
                                                                    If transcript is empty or too short, set low scores and mention in feedback.";
        public AssessmentService(ITranscriptionService transcriptionService, IConfiguration configuration, IUnitOfWork unitOfWork)
        {
            _transcriptionService = transcriptionService;
            _configuration = configuration;
            _unitOfWork = unitOfWork;
        }
        public async Task<AssessmentResult> EvaluateSpeakingAsync(AssessmentRequest req, CancellationToken ct = default)
        {
            try
            {
                var submission = await _unitOfWork.ExerciseSubmissions.GetByIdAsync(req.ExerciseSubmissionId);
                if (submission == null)
                    return CreateDefaultAssessmentResult();

                var exercise = await _unitOfWork.Exercises.GetByIdAsync(submission.ExerciseId);
                if (exercise == null)
                    return CreateDefaultAssessmentResult();

                string[] imageUrls = ExtractImageUrls(exercise.MediaUrl);
                var imageResponse = await DescribeImagesByAzureAsync(imageUrls);

                var studentTranscript = await TranscribeSpeechByGeminiAsync(req.Audio);

                string template = req.LanguageCode switch
                {
                    "ja" => "Language: ja\r\nExerciseType: {exerciseType}\r\nExercise: {exerciseText}\r\nImage-extracted text: {visionExtract}\r\n\r\nStudent transcript: {transcript}\r\n\r\n日本語のスピーキングの採点を行ってください。発音・流暢さ・一貫性（まとまり）・課題への正確さ・イントネーション・文法・語彙を評価し、CEFRに基づき総合レベルを決定してください。結果は必ずJSONのみで返してください（英語か日本語でもOK）。\r\nSame JSON fields as English.\r\n",
                    "zh" => "Language: zh\r\nExercise: {exerciseType}\r\nExercise: {exerciseText}\r\nImage-extracted text: {visionExtract}\r\n\r\nStudent transcript: {transcript}\r\n\r\n请对中文口语做评分：发音、流利度、连贯性、作答的准确性、语调、语法、词汇，按 CEFR 判定水平并给出反馈。只返回 JSON。\r\nSame JSON fields as English.\r\n",
                    _ => "Language: en\r\nExerciseType: {exerciseType}\r\nExercise: {exerciseText}\r\nImage-extracted text: {visionExtract}\r\n\r\nStudent transcript: {transcript}\r\n\r\nYou are grading an English speaking response. Evaluate pronunciation, fluency, coherence (logical structure & relevance), task accuracy (did they satisfy the exercise requirements), intonation (prosody), grammar accuracy and vocabulary richness. Use CEFR scale for overall level.\r\n\r\nReturn ONLY valid JSON:\r\n{\r\n  \"pronunciation\": 0,\r\n  \"fluency\": 0,\r\n  \"coherence\": 0,\r\n  \"accuracy\": 0,\r\n  \"intonation\": 0,\r\n  \"grammar\": 0,\r\n  \"vocabulary\": 0,\r\n  \"cefr_level\": \"A1\",\r\n  \"overall\": 0,\r\n  \"feedback\": \"...\",\r\n  \"transcript\": \"...\"\r\n}\r\n"
                };

                string userPrompt = template
                    .Replace("{exerciseType}", exercise.Type.ToString())
                    .Replace("{exerciseText}", exercise.Title + " " + exercise.Content ?? "")
                    .Replace("{visionExtract}", (imageResponse.IsSuccess) ? imageResponse.Content : "Invalid response")
                    .Replace("{transcript}", (studentTranscript.IsSuccess) ? studentTranscript.Content : "Invalid response");

                string deploymentName = _configuration["AzureOpenAISettings:ChatDeployment"];
                string endpoint = _configuration["AzureOpenAISettings:Endpoint"];
                string key = _configuration["AzureOpenAISettings:ApiKey"];

                AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                var chatClient = openAIClient.GetChatClient(deploymentName);
                var textPart = ChatMessageContentPart.CreateTextPart(userPrompt);
                var chatMessages = new List<ChatMessage>
                    {
                        new SystemChatMessage(MULTILINGUAL_SCORING_SYSTEM_PROMPT + template),
                        new UserChatMessage(textPart)
                    };
                ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages);
                Console.WriteLine($"[AI]: {chatCompletion}");
                var assessmentResult = ParseAIResponseToAssessmentResult(chatCompletion.Content[0].Text, req.LanguageCode);
                return assessmentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EvaluateSpeakingAsync: {ex.Message}");
                return CreateDefaultAssessmentResult();
            }
        }
        #region
        private AssessmentResult ParseAIResponseToAssessmentResult(string aiResponse, string languageCode)
        {
            try
            {
                var jsonStart = aiResponse.IndexOf('{');
                var jsonEnd = aiResponse.LastIndexOf('}') + 1;

                if (jsonStart >= 0 && jsonEnd > jsonStart)
                {
                    var jsonContent = aiResponse.Substring(jsonStart, jsonEnd - jsonStart);

                    var jsonObject = JsonSerializer.Deserialize<JsonElement>(jsonContent);

                    var result = new AssessmentResult();

                    if (jsonObject.TryGetProperty("pronunciation", out var pronunciation))
                        result.Scores.Pronunciation = pronunciation.GetInt32();

                    if (jsonObject.TryGetProperty("fluency", out var fluency))
                        result.Scores.Fluency = fluency.GetInt32();

                    if (jsonObject.TryGetProperty("coherence", out var coherence))
                        result.Scores.Coherence = coherence.GetInt32();

                    if (jsonObject.TryGetProperty("accuracy", out var accuracy))
                        result.Scores.Accuracy = accuracy.GetInt32();

                    if (jsonObject.TryGetProperty("intonation", out var intonation))
                        result.Scores.Intonation = intonation.GetInt32();

                    if (jsonObject.TryGetProperty("grammar", out var grammar))
                        result.Scores.Grammar = grammar.GetInt32();

                    if (jsonObject.TryGetProperty("vocabulary", out var vocabulary))
                        result.Scores.Vocabulary = vocabulary.GetInt32();

                    if (jsonObject.TryGetProperty("cefr_level", out var cefrLevel))
                        result.CefrLevel = cefrLevel.GetString() ?? "A1";

                    if (jsonObject.TryGetProperty("overall", out var overall))
                        result.Overall = overall.GetInt32();

                    if (jsonObject.TryGetProperty("feedback", out var feedback))
                        result.Feedback = feedback.GetString() ?? "";

                    if (jsonObject.TryGetProperty("transcript", out var transcript))
                        result.Transcript = transcript.GetString() ?? "";

                    SetLanguageSpecificLevels(result, languageCode);

                    return result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing AI response: {ex.Message}");
            }

            return CreateDefaultAssessmentResult();
        }
        private AssessmentResult CreateDefaultAssessmentResult()
        {
            return new AssessmentResult
            {
                Scores = new ExtendedScores
                {
                    Pronunciation = 5,
                    Fluency = 5,
                    Coherence = 5,
                    Accuracy = 5,
                    Intonation = 5,
                    Grammar = 5,
                    Vocabulary = 5
                },
                CefrLevel = "A1",
                Overall = 5,
                Feedback = "Unable to evaluate at this time. Please try again.",
                Transcript = "Unable to transcribe."
            };
        }
        private void SetLanguageSpecificLevels(AssessmentResult result, string languageCode)
        {
            var overall = result.Overall;

            switch (languageCode)
            {
                case "ja":
                    result.JlptLevel = GetJlptLevel(overall);
                    break;
                case "zh":
                    result.HskLevel = GetHskLevel(overall);
                    break;
                case "en":
                    result.CefrLevel = GetEnLevel(overall);
                    break;
            }
        }
        private string GetEnLevel(int score)
        {
            return score switch
            {
                >= 90 => "C2",
                >= 80 => "C1",
                >= 70 => "B2",
                >= 60 => "B1",
                >= 50 => "A2",
                _ => "A1"
            };
        }
        private string GetJlptLevel(int score)
        {
            return score switch
            {
                >= 90 => "N1",
                >= 80 => "N2",
                >= 70 => "N3",
                >= 60 => "N4",
                _ => "N5"
            };
        }
        private string GetHskLevel(int score)
        {
            return score switch
            {
                >= 90 => "HSK6",
                >= 80 => "HSK5",
                >= 70 => "HSK4",
                >= 60 => "HSK3",
                >= 50 => "HSK2",
                _ => "HSK1"
            };
        }
        private async Task<CommonResponse> TranscribeSpeechByAzureAsync(IFormFile audio)
        {
            if (audio == null || audio.Length == 0)
                return new CommonResponse { IsSuccess = false, Content = "Invalid audio file." };

            try
            {
                await using var ms = new MemoryStream();
                await audio.CopyToAsync(ms);

                var audioBytes = ms.ToArray();
                var fileName = string.IsNullOrWhiteSpace(audio.FileName) ? "audio.wav" : audio.FileName;
                var contentType = string.IsNullOrWhiteSpace(audio.ContentType) ? "audio/wav" : audio.ContentType;

                var response = await _transcriptionService.TranscribeAsync(
                    audioBytes,
                    fileName,
                    contentType,
                    null
                );

                return !string.IsNullOrWhiteSpace(response)
                    ? new CommonResponse { IsSuccess = true, Content = response }
                    : new CommonResponse { IsSuccess = false, Content = "Transcription failed: empty response." };
            }
            catch (Exception ex)
            {
                return new CommonResponse { IsSuccess = false, Content = $"Transcription error: {ex.Message}" };
            }
        }
        private async Task<CommonResponse> TranscribeSpeechByGeminiAsync(IFormFile audio)
        {
            string apiKey = "AIzaSyAsAA8w7QSEdW5k2CcbuWuEhXLV17hiYHI";
            if (audio == null || audio.Length == 0)
                return new CommonResponse { IsSuccess = false, Content = "Invalid audio file." };

            byte[] audioBytes;
            using (var ms = new MemoryStream())
            {
                await audio.CopyToAsync(ms);
                audioBytes = ms.ToArray();
            }

            string audioBase64 = Convert.ToBase64String(audioBytes);
            var payload = new
            {
                contents = new[]
                {
                    new {
                        parts = new object[]
                        {
                            new { text = "Please transcribe this audio into text accurately:" },
                            new {
                                inline_data = new {
                                    mime_type = audio.ContentType,
                                    data = audioBase64
                                }
                            }
                        }
                    }
                }
            };
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);
            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
            var response = await client.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                var transcript = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return !string.IsNullOrWhiteSpace(transcript) ?
                    new CommonResponse { IsSuccess = true, Content = transcript }
                    : new CommonResponse { IsSuccess = false, Content = "No response" };
            }
            catch
            {
                return new CommonResponse { IsSuccess = false, Content = "Failed to parse transcript." };
            }
        }
        private async Task<CommonResponse> DescribeImagesByAzureAsync(string[] imageUrls)
        {
            try
            {
                if (imageUrls == null || imageUrls.Length == 0)
                    return new CommonResponse { IsSuccess = false, Content = "No images provided." };

                string deploymentName = _configuration["AzureOpenAISettings:ChatDeployment"];
                string endpoint = _configuration["AzureOpenAISettings:Endpoint"];
                string key = _configuration["AzureOpenAISettings:ApiKey"];
                AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                var chatClient = openAIClient.GetChatClient(deploymentName);

                var textPart = ChatMessageContentPart.CreateTextPart("Describe each image briefly in 1-2 sentences.");

                var imageParts = imageUrls.Select(url => ChatMessageContentPart.CreateImagePart(new Uri(url))).ToList();

                var userMessage = new UserChatMessage(new List<ChatMessageContentPart> { textPart }.Concat(imageParts));

                var chatMessages = new List<ChatMessage>
                    {
                        new SystemChatMessage("You are a helpful assistant."),
                        userMessage
                    };

                ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages);

                string result = chatCompletion.Content[0].Text;
                Console.WriteLine($"[ASSISTANT]: {result}");
                return !string.IsNullOrWhiteSpace(result) ? new CommonResponse { IsSuccess = true, Content = result }
                : new CommonResponse { IsSuccess = false, Content = result };
            }
            catch (Exception ex)
            {
                return new CommonResponse { IsSuccess = false, Content = $"Error: {ex.Message}" };
            }

        }
        private async Task<CommonResponse> DescribeImagesByGeminiAsync(string[] imageUrls)
        {
            if (imageUrls == null || imageUrls.Length == 0)
                return new CommonResponse { IsSuccess = false, Content = "No images provided." };

            string apiKey = "AIzaSyAsAA8w7QSEdW5k2CcbuWuEhXLV17hiYHI";
            var descriptions = new List<string>();

            using var client = new HttpClient();
            if (!client.DefaultRequestHeaders.Contains("x-goog-api-key"))
                client.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            foreach (var url in imageUrls)
            {
                var imageBytes = await client.GetByteArrayAsync(url);
                string imageBase64 = Convert.ToBase64String(imageBytes);

                string extension = Path.GetExtension(url).ToLower();
                string mimeType = extension switch
                {
                    ".jpg" or ".jpeg" => "image/jpeg",
                    ".png" => "image/png",
                    ".gif" => "image/gif",
                    ".webp" => "image/webp",
                    ".bmp" => "image/bmp",
                    _ => "application/octet-stream"
                };

                var payload = new
                {
                    contents = new[]
                    {
                        new {
                            parts = new object[]
                            {
                                        new { text = "Describe each image briefly in 1-2 sentences." },
                                        new { inline_data = new { mime_type = mimeType, data = imageBase64 } }
                                    }
                                }
                            }
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");

                var urlApi = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
                using var response = await client.PostAsync(urlApi, content);
                var responseText = await response.Content.ReadAsStringAsync();

                try
                {
                    using var doc = JsonDocument.Parse(responseText);
                    var description = doc.RootElement
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    descriptions.Add(description ?? "No description");
                }
                catch
                {
                    descriptions.Add("Failed to parse description.");
                }
            }

            var result = string.Join("\n---\n", descriptions);
            return !string.IsNullOrWhiteSpace(result) ? new CommonResponse { IsSuccess = true, Content = result } : new CommonResponse { IsSuccess = false, Content = "No response" };
        }
        private int ComputeOverall(ExtendedScores s)
        {
            double overall = s.Pronunciation * 0.15 + s.Fluency * 0.20 + s.Coherence * 0.20
                + s.Accuracy * 0.25 + s.Intonation * 0.10 + s.Grammar * 0.05 + s.Vocabulary * 0.05;
            return (int)Math.Round(overall / 10.0 * 10);
        }
        private string[] ExtractImageUrls(string input)
        {
            return input.Split(';', StringSplitOptions.RemoveEmptyEntries);
        }
        #endregion
    }
}
