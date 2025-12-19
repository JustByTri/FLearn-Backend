using Azure;
using Azure.AI.OpenAI;
using BLL.IServices.Assessment;
using BLL.Settings;
using Common.DTO.Assessment.Response;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
using System.Text;
using System.Text.Json;

namespace BLL.Services.Assessment
{
    public class AssessmentService : IAssessmentService
    {
        private static IConfiguration _configuration;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPronunciationService _pronunciationService;
        private static GeminiSettings _geminiSettings;
        private const string MULTILINGUAL_SCORING_SYSTEM_PROMPT = @"You are an expert speaking examiner.
                                                                    Your task: Grade the student's speaking submission based on the provided transcript and context.
                                                                    
                                                                    CRITICAL OUTPUT RULES:
                                                                    1. Return ONLY valid JSON. No markdown, no introductory text.
                                                                    2. The 'feedback' field MUST be in VIETNAMESE (Tiếng Việt).
                                                                    3. Do NOT include a 'transcript' field in the output.
                                                                    
                                                                    JSON Schema Example:
                                                                    {
                                                                      ""pronunciation"": 85,
                                                                      ""fluency"": 80,
                                                                      ""coherence"": 75,
                                                                      ""accuracy"": 90,
                                                                      ""intonation"": 70,
                                                                      ""grammar"": 85,
                                                                      ""vocabulary"": 80,
                                                                      ""cefr_level"": ""B1"",
                                                                      ""overall"": 82,
                                                                      ""feedback"": ""Bạn phát âm khá tốt, tuy nhiên cần chú ý âm đuôi (ending sounds) ở các từ số nhiều. Ngữ điệu còn hơi phẳng, hãy thử lên xuống giọng tự nhiên hơn.""
                                                                    }
                                                                    ";
        public AssessmentService(IConfiguration configuration, IUnitOfWork unitOfWork, IPronunciationService pronunciationService, IOptions<GeminiSettings> geminiSettings)
        {
            _configuration = configuration;
            _unitOfWork = unitOfWork;
            _pronunciationService = pronunciationService;
            _geminiSettings = geminiSettings.Value;
        }
        public async Task<AssessmentResult> EvaluateSpeakingAsync(AssessmentRequest req)
        {
            try
            {
                var submissionTask = _unitOfWork.ExerciseSubmissions.GetByIdAsync(req.ExerciseSubmissionId);
                var exerciseTask = submissionTask.ContinueWith(async t =>
                {
                    var submission = await t;
                    return submission != null ? await _unitOfWork.Exercises.GetByIdAsync(submission.ExerciseId) : null;
                }).Unwrap();

                await Task.WhenAll(submissionTask, exerciseTask);

                var submission = await submissionTask;
                var exercise = await exerciseTask;

                if (submission == null || exercise == null)
                    return CreateFallbackResult("Dữ liệu bài tập không tồn tại.", req.LanguageCode);

                if (exercise.Type == SpeakingExerciseType.RepeatAfterMe)
                {
                    string referenceText = !string.IsNullOrEmpty(exercise.Content) ? exercise.Content : exercise.Prompt;

                    if (string.IsNullOrEmpty(referenceText))
                        return CreateFallbackResult("Không tìm thấy văn bản mẫu để chấm điểm.", req.LanguageCode);

                    var result = await _pronunciationService.AssessPronunciationAsync(submission.AudioUrl, referenceText, req.LanguageCode);

                    if (result != null)
                    {
                        return _pronunciationService.ConvertToAssessmentResult(result, referenceText, req.LanguageCode);
                    }
                    else
                    {
                        return CreateFallbackResult("Không thể nhận diện giọng nói hoặc âm thanh quá ồn.", req.LanguageCode);
                    }
                }

                var studentTranscript = await TranscribeSpeechByAzureAsync(req.AudioUrl, req.LanguageCode);

                if (!studentTranscript.IsSuccess)
                {
                    Console.WriteLine($"[Azure Speech Failed]: {studentTranscript.Content}. Switching to Gemini Transcribe...");
                    studentTranscript = await TranscribeSpeechByGeminiAsync(req.AudioUrl);
                }

                if (!studentTranscript.IsSuccess)
                {
                    return CreateFallbackResult("Không thể nhận diện giọng nói.", req.LanguageCode);
                }

                List<string> imageUrls = new List<string>();
                if (!string.IsNullOrEmpty(exercise.MediaUrl) &&
                   (exercise.Type == SpeakingExerciseType.PictureDescription ||
                    exercise.Type == SpeakingExerciseType.StoryTelling ||
                    exercise.Type == SpeakingExerciseType.Debate))
                {
                    imageUrls = ExtractImageUrls(exercise.MediaUrl).Take(3).ToList();
                }

                string textInstruction = req.LanguageCode switch
                {
                    "ja" => GenerateJapanesePrompt(exercise, studentTranscript),
                    "zh" => GenerateChinesePrompt(exercise, studentTranscript),
                    _ => GenerateEnglishPrompt(exercise, studentTranscript)
                };

                AssessmentResult assessmentResult = new AssessmentResult();

                try
                {
                    string deploymentName = _configuration["AzureOpenAISettings:ChatDeployment"];
                    string endpoint = _configuration["AzureOpenAISettings:Endpoint"];
                    string key = _configuration["AzureOpenAISettings:ApiKey"];

                    AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                    var chatClient = openAIClient.GetChatClient(deploymentName);

                    var messageContentParts = new List<ChatMessageContentPart>
                    {
                        ChatMessageContentPart.CreateTextPart(textInstruction)
                    };

                    foreach (var url in imageUrls)
                    {
                        messageContentParts.Add(ChatMessageContentPart.CreateImagePart(new Uri(url), ChatImageDetailLevel.Low));
                    }

                    var chatMessages = new List<ChatMessage>
                    {
                        new SystemChatMessage(MULTILINGUAL_SCORING_SYSTEM_PROMPT),
                        new UserChatMessage(messageContentParts)
                    };

                    ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages);

                    if (chatCompletion == null || chatCompletion.Content.Count == 0)
                        throw new Exception("Azure returned empty response.");

                    assessmentResult = ParseAIResponseToAssessmentResult(chatCompletion.Content[0].Text, req.LanguageCode);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Azure AI ERROR]: {ex.Message}. Switching to Gemini...");
                    try
                    {
                        assessmentResult = await EvaluateSpeakingByGeminiAsync(textInstruction, imageUrls, req.LanguageCode);
                    }
                    catch (Exception geminiEx)
                    {
                        Console.WriteLine($"[Gemini Fallback FAILED]: {geminiEx.Message}");
                        return CreateFallbackResult("Hệ thống AI đang bận. Điểm số được ghi nhận cho nỗ lực.", req.LanguageCode);
                    }
                }

                assessmentResult.Overall = Math.Clamp(assessmentResult.Overall, 0, 100);
                assessmentResult.RecognizedText = studentTranscript.Content;
                assessmentResult.Transcript = null;

                return assessmentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EvaluateSpeakingAsync: {ex.Message}");
                return CreateFallbackResult("Hệ thống gặp sự cố gián đoạn.", req.LanguageCode);
            }
        }
        #region
        private AssessmentResult ParseAIResponseToAssessmentResult(string aiResponse, string languageCode)
        {
            try
            {
                string cleanJson = aiResponse;
                if (cleanJson.Contains("```json"))
                {
                    cleanJson = cleanJson.Replace("```json", "").Replace("```", "");
                }
                else if (cleanJson.Contains("```"))
                {
                    cleanJson = cleanJson.Replace("```", "");
                }

                cleanJson = cleanJson.Trim();

                var jsonStart = cleanJson.IndexOf('{');
                var jsonEndIndex = cleanJson.LastIndexOf('}');

                if (jsonStart == -1 || jsonEndIndex == -1 || jsonEndIndex < jsonStart)
                {
                    Console.WriteLine($"[AI JSON ERROR] Raw: {aiResponse}");
                    var fallback = CreateFallbackResult("Hệ thống AI đang bận hoặc gặp sự cố gián đoạn. Điểm số được ghi nhận cho nỗ lực hoàn thành bài tập.", languageCode);
                    fallback.Feedback = "AI output format error. Raw: " + aiResponse.Substring(0, Math.Min(50, aiResponse.Length));
                    return fallback;
                }

                cleanJson = cleanJson.Substring(jsonStart, jsonEndIndex - jsonStart + 1);

                var jsonObject = JsonSerializer.Deserialize<JsonElement>(cleanJson);

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

                SetLanguageSpecificLevels(result, languageCode);

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FINAL JSON PARSE FAIL]: {ex.Message}");
                throw;
            }
        }
        private AssessmentResult CreateFallbackResult(string feedbackReason, string languageCode)
        {
            int fallbackScore = 35;

            var result = new AssessmentResult
            {
                Scores = new ExtendedScores
                {
                    Pronunciation = fallbackScore,
                    Fluency = fallbackScore,
                    Coherence = fallbackScore,
                    Accuracy = fallbackScore,
                    Intonation = fallbackScore,
                    Grammar = fallbackScore,
                    Vocabulary = fallbackScore
                },
                Overall = fallbackScore,
                Feedback = feedbackReason,
                Transcript = "Chất lượng âm thanh không rõ hoặc hệ thống gặp lỗi khi xử lý."
            };

            SetLanguageSpecificLevels(result, languageCode);

            if (string.IsNullOrEmpty(result.CefrLevel))
            {
                result.CefrLevel = "A1";
            }

            return result;
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
        public static async Task<CommonResponse> TranscribeSpeechByGeminiAsync(string audioUrl, CancellationToken cancellationToken = default)
        {
            var apiKeys = _geminiSettings.ApiKeys;

            if (apiKeys == null || apiKeys.Count == 0)
            {
                return new CommonResponse { IsSuccess = false, Content = "Gemini API Keys are missing in configuration." };
            }

            if (string.IsNullOrWhiteSpace(audioUrl))
                return new CommonResponse { IsSuccess = false, Content = "Invalid audio URL." };

            byte[] audioBytes;
            string mimeType;

            using (var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(180) })
            {
                try
                {
                    audioBytes = await httpClient.GetByteArrayAsync(audioUrl, cancellationToken);
                    mimeType = GetMimeType(audioUrl);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error downloading audio: {ex.Message}");
                    return new CommonResponse { IsSuccess = false, Content = "Failed to download audio." };
                }
            }

            string audioBase64 = Convert.ToBase64String(audioBytes);

            for (int i = 0; i < apiKeys.Count; i++)
            {
                string currentKey = apiKeys[i];
                Console.WriteLine($"[Gemini Transcribe] Trying Key #{i + 1}...");

                try
                {
                    var payload = new
                    {
                        contents = new[]
                        {
                            new {
                                    parts = new object[]
                                    {
                                        new
                                        {
                                            text = "Please transcribe this audio into text accurately. Return only the transcription without any additional text:"
                                        },
                                        new
                                        {
                                            inline_data = new
                                            {
                                                mime_type = mimeType,
                                                data = audioBase64
                                            }
                                        }
                                    }
                            }
                        },
                        generationConfig = new
                        {
                            maxOutputTokens = 1000,
                            temperature = 0.1
                        }
                    };

                    var json = JsonSerializer.Serialize(payload);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    using var geminiClient = new HttpClient();
                    geminiClient.Timeout = TimeSpan.FromSeconds(30);

                    geminiClient.DefaultRequestHeaders.Remove("x-goog-api-key");
                    geminiClient.DefaultRequestHeaders.Add("x-goog-api-key", currentKey);

                    var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";

                    var response = await geminiClient.PostAsync(url, content, cancellationToken);
                    var responseText = await response.Content.ReadAsStringAsync(cancellationToken);

                    if (response.IsSuccessStatusCode)
                    {
                        using var doc = JsonDocument.Parse(responseText);
                        var candidates = doc.RootElement.GetProperty("candidates");
                        if (candidates.GetArrayLength() > 0)
                        {
                            var transcript = candidates[0]
                                .GetProperty("content")
                                .GetProperty("parts")[0]
                                .GetProperty("text")
                                .GetString()?
                                .Trim();

                            return !string.IsNullOrWhiteSpace(transcript)
                                ? new CommonResponse { IsSuccess = true, Content = transcript }
                                : new CommonResponse { IsSuccess = false, Content = "Empty transcription" };
                        }
                    }

                    Console.WriteLine($"[Gemini Key #{i + 1} Failed]: {response.StatusCode} - {responseText}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Key #{i + 1} Exception]: {ex.Message}");
                }
            }

            return new CommonResponse { IsSuccess = false, Content = "All Gemini API keys failed." };
        }
        public static async Task<CommonResponse> TranscribeSpeechByAzureAsync(string audioUrl, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(audioUrl))
                return new CommonResponse { IsSuccess = false, Content = "Invalid audio URL." };

            Console.WriteLine($"[Azure Speech] Downloading audio from: {audioUrl}");

            byte[] audioBytes;
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(180) })
            {
                try
                {
                    audioBytes = await client.GetByteArrayAsync(audioUrl);
                    if (audioBytes == null || audioBytes.Length < 500)
                        return new CommonResponse { IsSuccess = false, Content = "Audio file is too short or empty." };
                }
                catch (Exception ex)
                {
                    return new CommonResponse { IsSuccess = false, Content = $"Audio download failed: {ex.Message}" };
                }
            }

            string tempPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.wav");

            try
            {
                await File.WriteAllBytesAsync(tempPath, audioBytes);

                try
                {
                    var speechKey = _configuration["SpeechSettings:ApiKey"];
                    var speechRegion = _configuration["SpeechSettings:Region"];
                    if (string.IsNullOrWhiteSpace(speechKey) || string.IsNullOrWhiteSpace(speechRegion))
                        return new CommonResponse { IsSuccess = false, Content = "Azure Speech configuration missing." };

                    var config = SpeechConfig.FromSubscription(speechKey, speechRegion);
                    config.SpeechRecognitionLanguage = MapToAzureLanguage(languageCode);

                    config.SetProperty(PropertyId.SpeechServiceConnection_InitialSilenceTimeoutMs, "5000");
                    config.SetProperty(PropertyId.Speech_SegmentationSilenceTimeoutMs, "2000");

                    int retryCount = 3;
                    for (int attempt = 1; attempt <= retryCount; attempt++)
                    {
                        try
                        {
                            Console.WriteLine($"[Azure Speech] Attempt {attempt}/3");

                            using var audioConfig = AudioConfig.FromWavFileInput(tempPath);
                            using var recognizer = new SpeechRecognizer(config, audioConfig);

                            var fullTranscript = new StringBuilder();
                            var stopRecognition = new TaskCompletionSource<int>();

                            recognizer.Recognized += (s, e) =>
                            {
                                if (e.Result.Reason == ResultReason.RecognizedSpeech)
                                {
                                    fullTranscript.Append(e.Result.Text + " ");
                                    Console.WriteLine($"[Azure Segment]: {e.Result.Text}");
                                }
                            };


                            recognizer.Canceled += (s, e) =>
                            {
                                Console.WriteLine($"[Azure Canceled] Reason: {e.Reason}");
                                if (e.Reason == CancellationReason.Error)
                                {
                                    Console.WriteLine($"ErrorDetails: {e.ErrorDetails}");
                                }
                                stopRecognition.TrySetResult(0);
                            };

                            recognizer.SessionStopped += (s, e) =>
                            {
                                Console.WriteLine("[Azure SessionStopped]");
                                stopRecognition.TrySetResult(0);
                            };

                            await recognizer.StartContinuousRecognitionAsync();

                            Task.WaitAny(new[] { stopRecognition.Task }, TimeSpan.FromSeconds(180));

                            await recognizer.StopContinuousRecognitionAsync();

                            var finalResult = fullTranscript.ToString().Trim();

                            if (string.IsNullOrWhiteSpace(finalResult))
                            {
                                return new CommonResponse { IsSuccess = false, Content = "No speech recognized (Empty)." };
                            }

                            return new CommonResponse { IsSuccess = true, Content = finalResult };
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Azure Speech] Exception on attempt {attempt}: {ex.Message}");
                            if (attempt == retryCount)
                                return new CommonResponse { IsSuccess = false, Content = $"Speech recognition failed: {ex.Message}" };
                        }

                        await Task.Delay(800);
                    }

                    return new CommonResponse { IsSuccess = false, Content = "Speech not recognized after retries." };
                }
                finally
                {
                    try
                    {
                        if (File.Exists(tempPath))
                        {
                            File.Delete(tempPath);
                            Console.WriteLine($"[Azure Speech] Deleted temp file: {tempPath}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[Azure Speech] Failed to delete temp file {tempPath}: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return new CommonResponse { IsSuccess = false, Content = $"Failed to save audio file: {ex.Message}" };
            }
        }
        private async Task<CommonResponse> DescribeImagesByAzureAsync(string[] imageUrls, CancellationToken cancellationToken = default)
        {
            try
            {
                if (imageUrls == null || imageUrls.Length == 0)
                    return new CommonResponse { IsSuccess = false, Content = "No images provided." };

                var imagesToProcess = imageUrls.Take(2).ToArray();

                string deploymentName = _configuration["AzureOpenAISettings:ChatDeployment"];
                string endpoint = _configuration["AzureOpenAISettings:Endpoint"];
                string key = _configuration["AzureOpenAISettings:ApiKey"];
                AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                var chatClient = openAIClient.GetChatClient(deploymentName);

                var textPart = ChatMessageContentPart.CreateTextPart("Describe each image briefly in 1-2 sentences. Focus on key elements that would be relevant for a speaking exercise.");

                var imageParts = imagesToProcess.Select(url => ChatMessageContentPart.CreateImagePart(new Uri(url))).ToList();

                var userMessage = new UserChatMessage(new List<ChatMessageContentPart> { textPart }.Concat(imageParts));

                var chatMessages = new List<ChatMessage>
                {
                    new SystemChatMessage("You are a helpful assistant that describes images concisely for language learning assessments."),
                    userMessage
                };

                ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages, cancellationToken: cancellationToken);

                string result = chatCompletion.Content[0].Text;
                Console.WriteLine($"[ASSISTANT]: {result}");
                return !string.IsNullOrWhiteSpace(result) ? new CommonResponse { IsSuccess = true, Content = result }
                : new CommonResponse { IsSuccess = false, Content = "No description generated" };
            }
            catch (OperationCanceledException)
            {
                return new CommonResponse { IsSuccess = false, Content = "Image description timeout." };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error describing images: {ex.Message}");
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
        private async Task<AssessmentResult> EvaluateSpeakingByGeminiAsync(string promptInstruction, List<string> imageUrls, string languageCode)
        {
            var apiKeys = _geminiSettings.ApiKeys;

            if (apiKeys == null || apiKeys.Count == 0)
                throw new Exception("Gemini API Keys are missing configuration.");

            var parts = new List<object>();
            string combinedPrompt = $"{MULTILINGUAL_SCORING_SYSTEM_PROMPT}\n\n---\n\n{promptInstruction}";
            parts.Add(new { text = combinedPrompt });

            if (imageUrls != null && imageUrls.Any())
            {
                using var imgClient = new HttpClient();
                foreach (var imgUrl in imageUrls)
                {
                    try
                    {
                        byte[] imageBytes = await imgClient.GetByteArrayAsync(imgUrl);
                        parts.Add(new
                        {
                            inline_data = new
                            {
                                mime_type = GetImageMimeType(imgUrl),
                                data = Convert.ToBase64String(imageBytes)
                            }
                        });
                    }
                    catch { /* Ignore bad images */ }
                }
            }

            var payload = new
            {
                contents = new[] { new { parts = parts.ToArray() } },
                generationConfig = new { temperature = 0.2, response_mime_type = "application/json" }
            };

            string jsonPayload = JsonSerializer.Serialize(payload);

            for (int i = 0; i < apiKeys.Count; i++)
            {
                string currentKey = apiKeys[i];
                Console.WriteLine($"[Gemini Grading] Trying Key #{i + 1}...");

                try
                {
                    string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={currentKey}";

                    using var client = new HttpClient();
                    client.Timeout = TimeSpan.FromSeconds(60);

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

                            var assessmentResult = ParseAIResponseToAssessmentResult(resultText, languageCode);
                            if (assessmentResult.Overall < 0) assessmentResult.Overall = 0;
                            if (assessmentResult.Overall > 100) assessmentResult.Overall = 100;

                            return assessmentResult;
                        }
                    }

                    Console.WriteLine($"[Gemini Grading Key #{i + 1} Failed]: {response.StatusCode} - {responseString}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Gemini Grading Key #{i + 1} Error]: {ex.Message}");
                    if (i < apiKeys.Count - 1) continue;
                }
            }

            throw new Exception("All Gemini API keys failed to grade submission.");
        }
        private string GetImageMimeType(string url)
        {
            var ext = Path.GetExtension(url).ToLower();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                ".heic" => "image/heic",
                ".heif" => "image/heif",
                _ => "image/jpeg"
            };
        }
        public static string GetMimeType(string url)
        {
            var ext = Path.GetExtension(url).ToLower();
            return ext switch
            {
                ".wav" => "audio/wav",
                ".mp3" => "audio/mpeg",
                ".m4a" => "audio/m4a",
                ".ogg" => "audio/ogg",
                _ => "audio/wav"
            };
        }
        private string[] ExtractImageUrls(string input)
        {
            return input.Split(';', StringSplitOptions.RemoveEmptyEntries);
        }
        private string GetEnglishExerciseType(SpeakingExerciseType type)
        {
            return type switch
            {
                SpeakingExerciseType.RepeatAfterMe => "repeat-after-me",
                SpeakingExerciseType.PictureDescription => "picture description",
                SpeakingExerciseType.StoryTelling => "story telling",
                SpeakingExerciseType.Debate => "debate",
                _ => "speaking"
            };
        }
        private string GetJapaneseExerciseType(SpeakingExerciseType type)
        {
            return type switch
            {
                SpeakingExerciseType.RepeatAfterMe => "リピートアフターミー",
                SpeakingExerciseType.PictureDescription => "絵の描写",
                SpeakingExerciseType.StoryTelling => "ストーリーテリング",
                SpeakingExerciseType.Debate => "ディベート",
                _ => "スピーキング"
            };
        }
        private string GetChineseExerciseType(SpeakingExerciseType type)
        {
            return type switch
            {
                SpeakingExerciseType.RepeatAfterMe => "跟读练习",
                SpeakingExerciseType.PictureDescription => "图片描述",
                SpeakingExerciseType.StoryTelling => "故事讲述",
                SpeakingExerciseType.Debate => "辩论",
                _ => "口语练习"
            };
        }
        private string GenerateEnglishPrompt(DAL.Models.Exercise exercise, CommonResponse studentTranscript)
        {
            var basePrompt = new StringBuilder();
            basePrompt.AppendLine("### CONTEXT");
            basePrompt.AppendLine($"Target Language: English (en)");
            basePrompt.AppendLine($"Exercise Type: {exercise.Type.ToString()}");
            basePrompt.AppendLine($"Exercise Title: {exercise.Title}");
            if (!string.IsNullOrEmpty(exercise.Prompt))
            {
                basePrompt.AppendLine($"Exercise Instruction: {exercise.Prompt}");
            }

            if (exercise.Type == SpeakingExerciseType.PictureDescription ||
                    exercise.Type == SpeakingExerciseType.StoryTelling)
            {
                basePrompt.AppendLine("VISUAL INPUT: I have attached image(s) related to this exercise. Please look at them carefully.");
            }

            basePrompt.AppendLine();
            basePrompt.AppendLine("### STUDENT INPUT (This is what the student said)");
            basePrompt.AppendLine($"Actual Speech Text: {(studentTranscript.IsSuccess ? studentTranscript.Content : "Audio unclear")}");
            basePrompt.AppendLine();

            basePrompt.AppendLine("### INSTRUCTIONS");
            basePrompt.Append($"You are an expert examiner grading an English {GetEnglishExerciseType(exercise.Type)} task. ");

            basePrompt.AppendLine("You must provide constructive feedback explaining the mistakes and how to improve in VIETNAMESE.");

            switch (exercise.Type)
            {
                case SpeakingExerciseType.RepeatAfterMe:
                    basePrompt.AppendLine("Focus on phonemic accuracy, intonation, stress, and rhythm. Compare the student's transcript to the expected content if provided. Ignore the attached images if they are irrelevant to pronunciation.");
                    break;

                case SpeakingExerciseType.PictureDescription:
                    basePrompt.AppendLine("Compare the student's description DIRECTLY with the attached image(s). Evaluate: 1) Accuracy (Is the description factually correct based on the image?), 2) Vocabulary (Did they use specific terms for objects/actions visible?), 3) Prepositions of place and spatial details.");
                    break;

                case SpeakingExerciseType.StoryTelling:
                    basePrompt.AppendLine("Assess how well the student constructs a narrative based on the attached image(s) (if any). Evaluate creativity, logical flow (beginning-middle-end), use of connecting words, and past tense consistency.");
                    break;

                case SpeakingExerciseType.Debate:
                    basePrompt.AppendLine("Evaluate the strength of arguments, logical consistency, use of persuasive language, and rebuttal skills. The visual input might be a prompt card - ensure they addressed the topic.");
                    break;

                default:
                    basePrompt.AppendLine("Evaluate overall speaking proficiency.");
                    break;
            }

            basePrompt.AppendLine(@"### SCORING
                                Evaluate based on CEFR standards (A1-C2).
                                Return ONLY valid JSON in the following format (no markdown, no explanation outside JSON):
                                {
                                  ""pronunciation"": 0-100,
                                  ""fluency"": 0-100,
                                  ""coherence"": 0-100,
                                  ""accuracy"": 0-100,
                                  ""intonation"": 0-100,
                                  ""grammar"": 0-100,
                                  ""vocabulary"": 0-100,
                                  ""cefr_level"": ""A1"",
                                  ""overall"": 0-100,
                                  ""feedback"": ""Nhận xét chi tiết bằng tiếng Việt, chỉ ra lỗi sai cụ thể và cách khắc phục..."",
                                }");

            return basePrompt.ToString();
        }
        private string GenerateJapanesePrompt(DAL.Models.Exercise exercise, CommonResponse studentTranscript)
        {
            var basePrompt = new StringBuilder();
            basePrompt.AppendLine("### コンテキスト (Context)");
            basePrompt.AppendLine($"言語: 日本語 (Japanese)");
            basePrompt.AppendLine($"課題タイプ: {exercise.Type.ToString()}");
            basePrompt.AppendLine($"課題タイトル: {exercise.Title}");
            if (!string.IsNullOrEmpty(exercise.Prompt))
            {
                basePrompt.AppendLine($"課題内容: {exercise.Prompt}");
            }

            if (exercise.Type == SpeakingExerciseType.PictureDescription ||
                exercise.Type == SpeakingExerciseType.StoryTelling)
            {
                basePrompt.AppendLine("視覚情報: この課題に関連する画像を添付しました。画像をよく見て採点してください。");
            }

            basePrompt.AppendLine();
            basePrompt.AppendLine("### 学生の発話内容");
            basePrompt.AppendLine($"音声認識結果: {(studentTranscript.IsSuccess ? studentTranscript.Content : "音声認識不可")}");
            basePrompt.AppendLine();

            basePrompt.AppendLine("### 採点指示 (Instructions)");
            basePrompt.Append($"あなたは日本語教育の専門家として、この{GetJapaneseExerciseType(exercise.Type)}の課題を評価します。");
            basePrompt.AppendLine("Provide feedback in VIETNAMESE (Tiếng Việt) so the student can understand.");

            switch (exercise.Type)
            {
                case SpeakingExerciseType.RepeatAfterMe:
                    basePrompt.AppendLine("発音の正確さ、アクセント（高低）、イントネーション、およびリズム（拍）を重点的に評価してください。お手本に近いかどうかを判定します。");
                    break;

                case SpeakingExerciseType.PictureDescription:
                    basePrompt.AppendLine("【重要】添付された画像と、学生の説明を直接比較してください。評価ポイント：1) 正確さ（画像にあるものを正しく説明しているか）、2) 語彙力（画像内の事物や動作に適した具体的な単語を使っているか）、3) 位置関係の表現（右、左、真ん中など）。");
                    break;

                case SpeakingExerciseType.StoryTelling:
                    basePrompt.AppendLine("添付画像（もしあれば）に基づいて、物語が論理的に構成されているかを評価してください。「起承転結」や接続詞の使用、テンス（時制）の一貫性をチェックしてください。");
                    break;

                case SpeakingExerciseType.Debate:
                    basePrompt.AppendLine("論理の構成、説得力、根拠の提示、および反論への対応能力を評価してください。");
                    break;

                default:
                    basePrompt.AppendLine("総合的なスピーキング能力を評価してください。");
                    break;
            }

            basePrompt.AppendLine(@"### 出力形式 (Scoring)
                                    JLPTレベル (N5-N1) を目安にCEFRレベルも判定してください。
                                    以下のJSON形式のみを返してください（Markdownや解説は不要）：
                                    {
                                      ""pronunciation"": 0-100,
                                      ""fluency"": 0-100,
                                      ""coherence"": 0-100,
                                      ""accuracy"": 0-100,
                                      ""intonation"": 0-100,
                                      ""grammar"": 0-100,
                                      ""vocabulary"": 0-100,
                                      ""cefr_level"": ""A1"",
                                      ""overall"": 0-100,
                                      ""feedback"": ""Nhận xét chi tiết bằng tiếng Việt về ngữ pháp, từ vựng và phát âm tiếng Nhật..."",
                                    }");

            return basePrompt.ToString();
        }
        private string GenerateChinesePrompt(DAL.Models.Exercise exercise, CommonResponse studentTranscript)
        {
            var basePrompt = new StringBuilder();
            basePrompt.AppendLine("### 背景信息 (Context)");
            basePrompt.AppendLine($"目标语言: 中文 (Chinese)");
            basePrompt.AppendLine($"练习类型: {exercise.Type.ToString()}");
            basePrompt.AppendLine($"练习标题: {exercise.Title}");
            if (!string.IsNullOrEmpty(exercise.Prompt))
            {
                basePrompt.AppendLine($"练习说明: {exercise.Prompt}");
            }

            if (exercise.Type == SpeakingExerciseType.PictureDescription ||
                exercise.Type == SpeakingExerciseType.StoryTelling)
            {
                basePrompt.AppendLine("视觉输入: 我附上了本次练习相关的图片，请务必参考图片内容进行评分。");
            }

            basePrompt.AppendLine();
            basePrompt.AppendLine("### 学生回答 (Student Input)");
            basePrompt.AppendLine($"语音转录: {(studentTranscript.IsSuccess ? studentTranscript.Content : "无法识别语音")}");
            basePrompt.AppendLine();

            basePrompt.AppendLine("### 评分指令 (Instructions)");
            basePrompt.Append($"请作为专业的中文口语考官，对这个{GetChineseExerciseType(exercise.Type)}练习进行评分。");
            basePrompt.AppendLine("Provide feedback in VIETNAMESE (Tiếng Việt).");

            switch (exercise.Type)
            {
                case SpeakingExerciseType.RepeatAfterMe:
                    basePrompt.AppendLine("重点评估声调（四声）的准确性、发音清晰度以及语流的自然程度。请忽略与发音无关的图片内容。");
                    break;

                case SpeakingExerciseType.PictureDescription:
                    basePrompt.AppendLine("【重要】请直接对比附带的图片和学生的描述。评估点：1) 准确性（是否如实描述了图片内容），2) 词汇丰富度（能否准确说出图片中物体和动作的名称），3) 方位词和量词的使用是否准确。");
                    break;

                case SpeakingExerciseType.StoryTelling:
                    basePrompt.AppendLine("评估学生基于图片（如有）构建故事的能力。关注叙事的连贯性、连接词的使用、情节发展的逻辑性以及语言的生动性。");
                    break;

                case SpeakingExerciseType.Debate:
                    basePrompt.AppendLine("评估论点是否鲜明、逻辑是否严密、论据是否充分，以及语言的说服力和反驳技巧。");
                    break;

                default:
                    basePrompt.AppendLine("评估整体口语水平。");
                    break;
            }

            basePrompt.AppendLine(@"### 输出格式 (Scoring)
                                    参考 HSK 标准进行评分。
                                    仅返回以下 JSON 格式（不要包含 Markdown 代码块或其他文字）：
                                    {
                                      ""pronunciation"": 0-100,
                                      ""fluency"": 0-100,
                                      ""coherence"": 0-100,
                                      ""accuracy"": 0-100,
                                      ""intonation"": 0-100,
                                      ""grammar"": 0-100,
                                      ""vocabulary"": 0-100,
                                      ""cefr_level"": ""A1"",
                                      ""overall"": 0-100,
                                      ""feedback"": ""Nhận xét chi tiết bằng tiếng Việt về thanh điệu, phát âm và dùng từ..."",
                                    }");

            return basePrompt.ToString();
        }
        private static string MapToAzureLanguage(string shortCode)
        {
            return shortCode?.ToLower() switch
            {
                "ja" => "ja-JP",
                "zh" => "zh-CN",
                "en" => "en-US",
                _ => "en-US"
            };
        }
        #endregion
    }
}
