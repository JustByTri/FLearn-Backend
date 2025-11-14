using Azure;
using Azure.AI.OpenAI;
using BLL.IServices.Assessment;
using BLL.Services.AI;
using Common.DTO.Assessment.Response;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using DAL.Type;
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
                var submissionTask = _unitOfWork.ExerciseSubmissions.GetByIdAsync(req.ExerciseSubmissionId);
                var exerciseTask = submissionTask.ContinueWith(async t =>
                {
                    var submission = await t;
                    return submission != null ? await _unitOfWork.Exercises.GetByIdAsync(submission.ExerciseId) : null;
                }, ct).Unwrap();

                await Task.WhenAll(submissionTask, exerciseTask);

                var submission = await submissionTask;
                var exercise = await exerciseTask;

                if (submission == null || exercise == null)
                    return CreateDefaultAssessmentResult();

                string? mediaContext = "";
                string? mediaType = "";

                if (exercise.Type == SpeakingExerciseType.RepeatAfterMe && !string.IsNullOrEmpty(exercise.MediaUrl))
                {
                    var audioResponse = await TranscribeSpeechByGeminiAsync(exercise.MediaUrl);
                    mediaContext = audioResponse.IsSuccess ? audioResponse.Content : "No reference audio available";
                    mediaType = "reference_audio";
                }
                else if ((exercise.Type == SpeakingExerciseType.PictureDescription ||
                         exercise.Type == SpeakingExerciseType.StoryTelling ||
                         exercise.Type == SpeakingExerciseType.Debate) &&
                         !string.IsNullOrEmpty(exercise.MediaUrl))
                {
                    string[] imageUrls = ExtractImageUrls(exercise.MediaUrl);
                    var imageResponse = await DescribeImagesByAzureAsync(imageUrls);
                    mediaContext = imageResponse.IsSuccess ? imageResponse.Content : "No image description available";
                    mediaType = "image_description";
                }
                else
                {
                    mediaContext = "No media reference provided";
                    mediaType = "none";
                }

                var studentTranscript = await TranscribeSpeechByGeminiAsync(req.AudioUrl);

                if (!studentTranscript.IsSuccess)
                {
                    return new AssessmentResult
                    {
                        Overall = 0,
                        Feedback = "Unable to transcribe student audio. Please check audio quality and try again."
                    };
                }

                string template = req.LanguageCode switch
                {
                    "ja" => GenerateJapanesePrompt(exercise, mediaContext, mediaType, studentTranscript),
                    "zh" => GenerateChinesePrompt(exercise, mediaContext, mediaType, studentTranscript),
                    _ => GenerateEnglishPrompt(exercise, mediaContext, mediaType, studentTranscript)
                };

                string deploymentName = _configuration["AzureOpenAISettings:ChatDeployment"];
                string endpoint = _configuration["AzureOpenAISettings:Endpoint"];
                string key = _configuration["AzureOpenAISettings:ApiKey"];

                AzureOpenAIClient openAIClient = new AzureOpenAIClient(new Uri(endpoint), new AzureKeyCredential(key));
                var chatClient = openAIClient.GetChatClient(deploymentName);
                var textPart = ChatMessageContentPart.CreateTextPart(template);
                var chatMessages = new List<ChatMessage>
                {
                        new SystemChatMessage(MULTILINGUAL_SCORING_SYSTEM_PROMPT),
                        new UserChatMessage(textPart)
                };

                ChatCompletion chatCompletion = await chatClient.CompleteChatAsync(chatMessages);
                Console.WriteLine($"[AI]: {chatCompletion}");

                var assessmentResult = ParseAIResponseToAssessmentResult(chatCompletion.Content[0].Text, req.LanguageCode);

                if (assessmentResult.Overall < 0) assessmentResult.Overall = 0;
                if (assessmentResult.Overall > 100) assessmentResult.Overall = 100;

                return assessmentResult;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in EvaluateSpeakingAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
        private async Task<CommonResponse> TranscribeSpeechByGeminiAsync(string audioUrl)
        {
            string apiKey = "AIzaSyAsAA8w7QSEdW5k2CcbuWuEhXLV17hiYHI";

            if (string.IsNullOrWhiteSpace(audioUrl))
                return new CommonResponse { IsSuccess = false, Content = "Invalid audio URL." };

            byte[] audioBytes;
            using (var client = new HttpClient())
            {
                try
                {
                    audioBytes = await client.GetByteArrayAsync(audioUrl);
                }
                catch
                {
                    return new CommonResponse { IsSuccess = false, Content = "Failed to download audio." };
                }
            }

            string audioBase64 = Convert.ToBase64String(audioBytes);

            string mimeType = GetMimeType(audioUrl);

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
                                    mime_type = mimeType,
                                    data = audioBase64
                                }
                            }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            using var geminiClient = new HttpClient();
            geminiClient.DefaultRequestHeaders.Add("x-goog-api-key", apiKey);

            var url = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent";
            var response = await geminiClient.PostAsync(url, content);
            var responseText = await response.Content.ReadAsStringAsync();
            Console.WriteLine(responseText);
            try
            {
                using var doc = JsonDocument.Parse(responseText);
                var transcript = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return !string.IsNullOrWhiteSpace(transcript)
                    ? new CommonResponse { IsSuccess = true, Content = transcript }
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
        private string GetMimeType(string url)
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
        private string GenerateEnglishPrompt(DAL.Models.Exercise exercise, string? mediaContext, string? mediaType, CommonResponse studentTranscript)
        {
            var basePrompt = new StringBuilder();
            basePrompt.AppendLine($"Language: en");
            basePrompt.AppendLine($"ExerciseType: {exercise.Type.ToString()}");
            basePrompt.AppendLine($"Exercise: {exercise.Title} {exercise.Content ?? ""}");

            if (mediaType == "image_description")
            {
                basePrompt.AppendLine($"Image description: {mediaContext}");
            }
            else if (mediaType == "reference_audio")
            {
                basePrompt.AppendLine($"Reference audio transcript: {mediaContext}");
            }

            basePrompt.AppendLine($"Student transcript: {(studentTranscript.IsSuccess ? studentTranscript.Content : "Invalid response")}");
            basePrompt.AppendLine();

            basePrompt.Append($"You are grading an English {GetEnglishExerciseType(exercise.Type)} speaking response. ");

            switch (exercise.Type)
            {
                case SpeakingExerciseType.RepeatAfterMe:
                    basePrompt.AppendLine("Focus on pronunciation accuracy, intonation matching, and how closely the student's speech matches the reference audio. Evaluate rhythm, stress patterns, and phonetic accuracy.");
                    break;
                case SpeakingExerciseType.PictureDescription:
                    basePrompt.AppendLine("Evaluate how accurately and comprehensively the student describes the image. Assess vocabulary usage for visual elements, logical structure of description, and relevance to the image content.");
                    break;
                case SpeakingExerciseType.StoryTelling:
                    basePrompt.AppendLine("Assess narrative structure, creativity, coherence, use of tenses, character development, and overall engagement in the storytelling.");
                    break;
                case SpeakingExerciseType.Debate:
                    basePrompt.AppendLine("Evaluate argument structure, persuasiveness, counter-argument handling, logical reasoning, rhetorical skills, and ability to present a coherent position.");
                    break;
            }

            basePrompt.AppendLine(@"
                    Evaluate pronunciation, fluency, coherence (logical structure & relevance), task accuracy (did they satisfy the exercise requirements), intonation (prosody), grammar accuracy and vocabulary richness. Use CEFR scale for overall level.

                    Return ONLY valid JSON:
                    {
                      ""pronunciation"": 0,
                      ""fluency"": 0,
                      ""coherence"": 0,
                      ""accuracy"": 0,
                      ""intonation"": 0,
                      ""grammar"": 0,
                      ""vocabulary"": 0,
                      ""cefr_level"": ""A1"",
                      ""overall"": 0,
                      ""feedback"": ""..."",
                      ""transcript"": ""...""
                    }");

            return basePrompt.ToString();
        }
        private string GenerateJapanesePrompt(DAL.Models.Exercise exercise, string? mediaContext, string? mediaType, CommonResponse studentTranscript)
        {
            var basePrompt = new StringBuilder();
            basePrompt.AppendLine($"Language: ja");
            basePrompt.AppendLine($"ExerciseType: {exercise.Type.ToString()}");
            basePrompt.AppendLine($"Exercise: {exercise.Title} {exercise.Content ?? ""}");

            if (mediaType == "image_description")
            {
                basePrompt.AppendLine($"画像の説明: {mediaContext}");
            }
            else if (mediaType == "reference_audio")
            {
                basePrompt.AppendLine($"参考音声の書き起こし: {mediaContext}");
            }

            basePrompt.AppendLine($"学生の回答: {(studentTranscript.IsSuccess ? studentTranscript.Content : "Invalid response")}");
            basePrompt.AppendLine();

            basePrompt.AppendLine($"この{GetJapaneseExerciseType(exercise.Type)}の課題を採点してください。");

            switch (exercise.Type)
            {
                case SpeakingExerciseType.RepeatAfterMe:
                    basePrompt.AppendLine("発音の正確さ、イントネーションの一致、参考音声との類似度を重点的に評価してください。リズム、アクセントパターン、音声の正確さを評価します。");
                    break;
                case SpeakingExerciseType.PictureDescription:
                    basePrompt.AppendLine("画像の描写がどれだけ正確かつ包括的であるかを評価してください。視覚的要素への語彙使用、説明の論理的構成、画像内容への関連性を評価します。");
                    break;
                case SpeakingExerciseType.StoryTelling:
                    basePrompt.AppendLine("物語の構成、創造性、一貫性、時制の使用、キャラクター展開、全体的な没入感を評価してください。");
                    break;
                case SpeakingExerciseType.Debate:
                    basePrompt.AppendLine("議論の構成、説得力、反論の扱い、論理的思考、修辞技術、一貫した立場の提示能力を評価してください。");
                    break;
            }

            basePrompt.AppendLine(@"
                                    発音・流暢さ・一貫性（まとまり）・課題への正確さ・イントネーション・文法・語彙を評価し、CEFRに基づき総合レベルを決定してください。

                                    必ずJSONのみを返してください：
                                    {
                                      ""pronunciation"": 0,
                                      ""fluency"": 0,
                                      ""coherence"": 0,
                                      ""accuracy"": 0,
                                      ""intonation"": 0,
                                      ""grammar"": 0,
                                      ""vocabulary"": 0,
                                      ""cefr_level"": ""A1"",
                                      ""overall"": 0,
                                      ""feedback"": ""..."",
                                      ""transcript"": ""...""
                                    }");

            return basePrompt.ToString();
        }
        private string GenerateChinesePrompt(DAL.Models.Exercise exercise, string? mediaContext, string? mediaType, CommonResponse studentTranscript)
        {
            var basePrompt = new StringBuilder();
            basePrompt.AppendLine($"Language: zh");
            basePrompt.AppendLine($"ExerciseType: {exercise.Type.ToString()}");
            basePrompt.AppendLine($"Exercise: {exercise.Title} {exercise.Content ?? ""}");

            if (mediaType == "image_description")
            {
                basePrompt.AppendLine($"图片描述: {mediaContext}");
            }
            else if (mediaType == "reference_audio")
            {
                basePrompt.AppendLine($"参考音频转录: {mediaContext}");
            }

            basePrompt.AppendLine($"学生回答: {(studentTranscript.IsSuccess ? studentTranscript.Content : "Invalid response")}");
            basePrompt.AppendLine();

            basePrompt.AppendLine($"请对这个{GetChineseExerciseType(exercise.Type)}口语练习做评分：");

            switch (exercise.Type)
            {
                case SpeakingExerciseType.RepeatAfterMe:
                    basePrompt.AppendLine("重点评估发音准确性、语调匹配度以及与参考音频的相似程度。评估节奏、重音模式和语音准确性。");
                    break;
                case SpeakingExerciseType.PictureDescription:
                    basePrompt.AppendLine("评估学生对图片描述的准确性和全面性。评估视觉元素的词汇使用、描述的逻辑结构以及与图片内容的相关性。");
                    break;
                case SpeakingExerciseType.StoryTelling:
                    basePrompt.AppendLine("评估叙事结构、创造性、连贯性、时态使用、角色发展和整体故事吸引力。");
                    break;
                case SpeakingExerciseType.Debate:
                    basePrompt.AppendLine("评估论点结构、说服力、反驳处理、逻辑推理、修辞技巧和提出连贯立场的能力。");
                    break;
            }

            basePrompt.AppendLine(@"
                                    发音、流利度、连贯性、作答的准确性、语调、语法、词汇，按 CEFR 判定水平并给出反馈。

                                    只返回 JSON：
                                    {
                                      ""pronunciation"": 0,
                                      ""fluency"": 0,
                                      ""coherence"": 0,
                                      ""accuracy"": 0,
                                      ""intonation"": 0,
                                      ""grammar"": 0,
                                      ""vocabulary"": 0,
                                      ""cefr_level"": ""A1"",
                                      ""overall"": 0,
                                      ""feedback"": ""..."",
                                      ""transcript"": ""...""
                                    }");

            return basePrompt.ToString();
        }
        #endregion
    }
}
