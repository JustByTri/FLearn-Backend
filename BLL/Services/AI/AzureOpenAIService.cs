using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Assement;
using Common.DTO.Conversation;
using Common.DTO.Learner;
using Common.DTO.Teacher;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text.Json;

namespace BLL.Services.AI
{
    public class AzureOpenAIService : IGeminiService
    {
        private readonly HttpClient _http;
        private readonly AzureOpenAISettings _settings;
        private readonly ILogger<AzureOpenAIService> _logger;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
        {
            PropertyNameCaseInsensitive = true
        };

        public AzureOpenAIService(HttpClient http, IOptions<AzureOpenAISettings> settings, ILogger<AzureOpenAIService> logger)
        {
            _http = http;
            _settings = settings.Value;
            _logger = logger;

            if (!string.IsNullOrWhiteSpace(_settings.Endpoint))
            {
                _http.BaseAddress = new Uri(_settings.Endpoint.TrimEnd('/') + "/");
            }
            _http.DefaultRequestHeaders.Remove("api-key");
            if (!string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _http.DefaultRequestHeaders.Add("api-key", _settings.ApiKey);
            }
        }

        private record ChatMessage(string role, string content);
        private sealed class ChatPayload
        {
            public object[] messages { get; set; } = Array.Empty<object>();
            public double temperature { get; set; }
            public int max_tokens { get; set; }
            public object? response_format { get; set; }
        }

        private async Task<string> ChatAsync(string systemPrompt, string userMessage, IEnumerable<string> history, bool jsonMode = false, int? maxTokens = null, double? temperature = null)
        {
            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(systemPrompt))
                messages.Add(new ChatMessage("system", systemPrompt));
            foreach (var h in history)
                messages.Add(new ChatMessage("user", h));
            messages.Add(new ChatMessage("user", userMessage));

            var deployment = string.IsNullOrWhiteSpace(_settings.ChatDeployment) ? "gpt-4o-mini" : _settings.ChatDeployment;
            var url = $"openai/deployments/{deployment}/chat/completions?api-version={_settings.ApiVersion}";

            // Lower token budget for JSON mode to reduce latency
            var chosenMaxTokens = maxTokens ?? (jsonMode ? 480 : Math.Max(256, Math.Min(_settings.MaxOutputTokens, 2048)));
            var chosenTemperature = temperature ?? _settings.Temperature;

            var body = new ChatPayload
            {
                messages = messages.ToArray(),
                temperature = chosenTemperature,
                max_tokens = chosenMaxTokens,
                response_format = jsonMode ? new { type = "json_object" } : null
            };
            var res = await _http.PostAsJsonAsync(url, body, _json);
            var txt = await res.Content.ReadAsStringAsync();
            if (!res.IsSuccessStatusCode)
            {
                _logger.LogError("Azure OpenAI error {Status}: {Text}", res.StatusCode, txt);
                throw new HttpRequestException($"Azure OpenAI error {res.StatusCode}: {txt}");
            }
            using var doc = JsonDocument.Parse(txt);
            var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            return content ?? string.Empty;
        }

        private static string BuildDifficultyRubric(string level)
        {
            if (string.IsNullOrWhiteSpace(level)) return "Simple to moderate complexity.";
            var l = level.ToUpperInvariant();
            return l switch
            {
                var x when x.Contains("A1") => "Very simple, short sentences, basic phrases.",
                var x when x.Contains("A2") => "Simple sentences, common phrases.",
                var x when x.Contains("B1") => "Everyday conversation, some detail, connected sentences.",
                var x when x.Contains("B2") => "Detailed scenario with problem-solving and opinions.",
                var x when x.Contains("C1") => "Complex, nuanced context and precise vocabulary.",
                var x when x.Contains("C2") => "Highly sophisticated, idiomatic contexts.",
                _ => "Moderate complexity appropriate to the level."
            };
        }

        private sealed class QuestionsWrapper { public List<VoiceAssessmentQuestion>? Questions { get; set; } }

        // Implement IGeminiService by mapping to chat completions
        public async Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context)
        {
            var rubric = BuildDifficultyRubric(context.DifficultyLevel);
            // Ask for shorter scenario and clear runtime rules
            var prompt = $@"Return a strict JSON object with keys:
- scenarioDescription (in {context.Language},60-100 words, include: time/place, participants, clear objective,1 cultural note, and1 plausible challenge; write as a compact paragraph, no bullets)
- aiRole (a short label in {context.Language}, max30 characters, no punctuation except spaces)
- systemPrompt (compact rules: reply ONLY in {context.Language}; no emojis/markdown; do NOT prefix with 'AI:' or a role name; stay strictly on topic '{context.Topic}'; keep replies concise:1–2 sentences; gently steer user back to topic if off-topic)
- firstMessage (in {context.Language},1–2 sentences, set the scene and invite a reply; no prefix labels)
- tasks (array of exactly3 objects with field taskDescription only; each is one imperative sentence in {context.Language}, max80 characters; Task1 easy → Task3 harder for level {context.DifficultyLevel}).

Topic: {context.Topic}
Language: {context.Language} ({context.LanguageCode})
Difficulty: {context.DifficultyLevel} — {rubric}
Scenario guidelines: {context.ScenarioGuidelines}
Roleplay notes: {context.RoleplayInstructions}
Evaluation focus: {context.EvaluationCriteria}
Output must be valid JSON only.";

            var json = await ChatAsync(context.MasterPrompt, prompt, Array.Empty<string>(), jsonMode: true);
            try
            {
                return JsonSerializer.Deserialize<GeneratedConversationContentDto>(json, _json) ?? new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice {context.Topic} in {context.Language} at {context.DifficultyLevel} level.",
                    AIRole = "Conversation Partner",
                    SystemPrompt = context.MasterPrompt,
                    FirstMessage = "Hello! Let's start!",
                    Tasks = new List<ConversationTaskDto>()
                };
            }
            catch
            {
                return new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice {context.Topic} in {context.Language} at {context.DifficultyLevel} level.",
                    AIRole = "Conversation Partner",
                    SystemPrompt = context.MasterPrompt,
                    FirstMessage = "Hello! Let's start!",
                    Tasks = new List<ConversationTaskDto>()
                };
            }
        }

        public async Task<string> GenerateResponseAsync(string systemPrompt, string userMessage, List<string> conversationHistory)
        => await ChatAsync(systemPrompt, userMessage, conversationHistory, jsonMode: false);

        public async Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt)
        {
            var json = await ChatAsync(string.Empty, evaluationPrompt, Array.Empty<string>(), jsonMode: true);
            try
            { return JsonSerializer.Deserialize<ConversationEvaluationResult>(json, _json) ?? new ConversationEvaluationResult(); }
            catch { return new ConversationEvaluationResult { AIFeedback = json }; }
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            var prompt = $"Translate the following text from {sourceLanguage} to {targetLanguage}. Only the translation.\n{text}";
            return await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: false);
        }

        public async Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(string languageCode, string languageName, string? programName = null)
        {
            // difficulty labels per language
            string[] labels = languageCode?.ToLowerInvariant().StartsWith("ja") == true
                ? new[] { "N5", "N4", "N3", "N2" }
                : languageCode?.ToLowerInvariant().StartsWith("zh") == true
                ? new[] { "HSK1", "HSK2", "HSK3", "HSK4" }
                : new[] { "A1", "A2", "B1", "B2" };

            string commonBan = "Do not use generic templates like 'Introduce yourself', 'Daily routine', 'Recent experience', 'Give an opinion', or 'about technology'. Do NOT include phrases like 'in English'/'in Japanese' in the question text. Write directly in the target language.";
            string lengths = "Enforce lengths: Q1 must be2-3 words; Q2 must be3-5 words; Q3 must be6-8 words (short phrase); Q4 must be8-12 words (short sentence).";
            string constraints = $"Return a JSON object with property 'questions' (array of exactly4). Each is a concise speaking prompt for {languageName} ({languageCode}), tailored to program '{programName}', with ascending difficulty strictly set to [{string.Join(',', labels)}] in order. {lengths} {commonBan}";
            string fields = "Each item fields: questionNumber (1..4), question, promptText (<=15 words), vietnameseTranslation, wordGuides (nullable), questionType='speaking', difficulty (one of the ordered labels), maxRecordingSeconds =30/60/90/120. Return JSON only.";
            var prompt1 = $"{constraints}\n{fields}";
            var json = await ChatAsync(string.Empty, prompt1, Array.Empty<string>(), jsonMode: true, maxTokens: 640, temperature: 0.25);
            _logger.LogInformation("AI questions raw JSON: {Json}", json);
            var list = TryParseQuestions(json);
            list = SanitizeQuestions(list, languageName);
            if (IsLowDiversity(list))
            {
                // Second attempt with stronger constraints and a bit more temperature for variety
                var prompt2 = $"{constraints} Use practical, real-life contexts for '{programName}' (e.g., meetings, pitches, negotiations, customer service, travel). Avoid duplicates. {fields}";
                var json2 = await ChatAsync(string.Empty, prompt2, Array.Empty<string>(), jsonMode: true, maxTokens: 680, temperature: 0.35);
                _logger.LogInformation("AI questions retry JSON: {Json}", json2);
                var list2 = TryParseQuestions(json2);
                list2 = SanitizeQuestions(list2, languageName);
                if (list2.Any()) return list2;
            }
            return list;
        }

        private List<VoiceAssessmentQuestion> TryParseQuestions(string json)
        {
            try
            {
                var wrap = JsonSerializer.Deserialize<QuestionsWrapper>(json, _json);
                if (wrap?.Questions != null && wrap.Questions.Any())
                {
                    _logger.LogInformation("Parsed AI questions: {Count}. First: {Q}", wrap.Questions.Count, wrap.Questions.First().Question);
                    return wrap.Questions;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse wrapped questions JSON");
            }
            try
            {
                var arr = JsonSerializer.Deserialize<List<VoiceAssessmentQuestion>>(json, _json) ?? new();
                if (arr.Any())
                {
                    _logger.LogInformation("Parsed AI questions from array: {Count}. First: {Q}", arr.Count, arr.First().Question);
                }
                return arr;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse AI questions as array");
                return new List<VoiceAssessmentQuestion>();
            }
        }

        private static List<VoiceAssessmentQuestion> SanitizeQuestions(List<VoiceAssessmentQuestion> items, string languageName)
        {
            if (items == null) return new();
            string[] phrases = new[]
            {
                $" in {languageName}", $" In {languageName}",
                " in English", " in Japanese", " in Chinese"
            };
            foreach (var q in items)
            {
                if (!string.IsNullOrEmpty(q.Question))
                {
                    foreach (var p in phrases)
                    {
                        q.Question = q.Question.Replace(p, string.Empty, StringComparison.OrdinalIgnoreCase).Trim();
                    }
                }
                if (string.IsNullOrWhiteSpace(q.QuestionType)) q.QuestionType = "speaking";
                if (q.MaxRecordingSeconds == 0) q.MaxRecordingSeconds = Math.Min(30 + (q.QuestionNumber - 1) * 30, 120);
            }
            return items;
        }

        private static bool IsLowDiversity(List<VoiceAssessmentQuestion> items)
        {
            if (items == null || items.Count < 4) return true;
            string[] bannedStarts = new[]
            {
                "Introduce yourself", "Daily routine", "Recent experience", "Give an opinion"
            };
            int similar = items.Count(q => bannedStarts.Any(b => (q.Question ?? string.Empty).StartsWith(b, StringComparison.OrdinalIgnoreCase)));
            return similar >= 2; // if >=2 items match banned templates, consider low-diversity
        }

        public async Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(List<VoiceAssessmentQuestion> questions, string languageCode, string languageName, List<string>? programLevelNames = null)
        {
            var qJson = JsonSerializer.Serialize(questions, _json);
            var scale = programLevelNames != null && programLevelNames.Any() ? $"Use levels: {string.Join(",", programLevelNames)}" : "Use CEFR/HSK/JLPT scale";
            var prompt = $"Evaluate batch voice responses for {languageName} ({languageCode}). Input questions JSON: {qJson}. Return JSON BatchVoiceEvaluationResult. {scale}.";
            var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 500, temperature: 0.2);
            try { return JsonSerializer.Deserialize<BatchVoiceEvaluationResult>(json, _json) ?? new BatchVoiceEvaluationResult(); } catch { return new BatchVoiceEvaluationResult { OverallScore = 0, OverallLevel = "Unknown" }; }
        }

        public async Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(VoiceAssessmentQuestion question, IFormFile audioFile, string languageCode)
        {
            var prompt = $"Evaluate recorded response for: {question.Question} ({question.Difficulty}). Provide JSON VoiceEvaluationResult.";
            var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 400, temperature: 0.2);
            try { return JsonSerializer.Deserialize<VoiceEvaluationResult>(json, _json) ?? new VoiceEvaluationResult(); } catch { return new VoiceEvaluationResult { OverallScore = 70 }; }
        }

        public async Task<AiCourseRecommendationDto> GenerateCourseRecommendationsAsync(UserSurveyResponseDto survey, List<CourseInfoDto> availableCourses)
        {
            var payload = new { survey, availableCourses };
            var prompt = $"Recommend3-5 courses for the user below. Return JSON AiCourseRecommendationDto.\n{JsonSerializer.Serialize(payload, _json)}";
            var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 600, temperature: 0.2);
            try { return JsonSerializer.Deserialize<AiCourseRecommendationDto>(json, _json) ?? new AiCourseRecommendationDto(); } catch { return new AiCourseRecommendationDto(); }
        }

        public async Task<string> GenerateStudyPlanAsync(UserSurveyResponseDto survey)
        {
            var prompt = $"Create a weekly study plan as plain text for this learner: {JsonSerializer.Serialize(survey, _json)}";
            return await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: false, maxTokens: 800, temperature: 0.4);
        }

        public async Task<List<string>> GenerateStudyTipsAsync(UserSurveyResponseDto survey)
        {
            var prompt = $"Return8-10 study tips as a plain text list (one per line) for this learner: {JsonSerializer.Serialize(survey, _json)}";
            var text = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: false, maxTokens: 300, temperature: 0.3);
            return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimStart('-', ' ', '•').Trim()).ToList();
        }

        public async Task<TeacherQualificationAnalysisDto> AnalyzeTeacherQualificationsAsync(TeacherApplicationDto application, List<TeacherCredentialDto> credentials)
        {
            var prompt = $"Analyze teacher qualifications. Return JSON TeacherQualificationAnalysisDto.\nApplication: {JsonSerializer.Serialize(application, _json)}\nCredentials: {JsonSerializer.Serialize(credentials, _json)}";
            var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 700, temperature: 0.2);
            try { return JsonSerializer.Deserialize<TeacherQualificationAnalysisDto>(json, _json) ?? new TeacherQualificationAnalysisDto(); } catch { return new TeacherQualificationAnalysisDto(); }
        }
    }
}