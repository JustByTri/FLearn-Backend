using Azure;
using Azure.AI.OpenAI;
using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Assement;
using Common.DTO.Conversation;
using Common.DTO.Learner;
using Common.DTO.Teacher;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI.Chat;
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
        private readonly AzureOpenAIClient _openAiClient;

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
            if (!string.IsNullOrWhiteSpace(_settings.Endpoint) && !string.IsNullOrWhiteSpace(_settings.ApiKey))
            {
                _openAiClient = new AzureOpenAIClient(
                    new Uri(_settings.Endpoint),
                    new AzureKeyCredential(_settings.ApiKey));
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

        // --- CODE ĐƯỢC CHỈNH SỬA TỪ ĐÂY ---

        public async Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context)
        {
            var languageConstraints = GetStrictLanguageConstraints(context.Language, context.DifficultyLevel);
            var vibes = new[] { "Urgent", "Relaxed", "Curious", "Formal", "Friendly" };
            var selectedVibe = vibes[Random.Shared.Next(vibes.Length)];

            string specificTopicInstruction = string.IsNullOrWhiteSpace(context.TopicContextPrompt)
                ? $"Your goal is to help the user practice {context.Topic}." : $"SCENARIO INSTRUCTION: {context.TopicContextPrompt}. strict adhere to this scenario.";

            var prompt = $@"
# ROLEPLAY GENERATION
**Target Language**: {context.Language}
**Topic**: {context.Topic}
**Level**: {context.DifficultyLevel}
**Vibe**: {selectedVibe}

# INSTRUCTIONS
Create a structured roleplay scenario in **{context.Language}**.
1. **ScenarioDescription**: Briefly describe the setting.
2. **SystemPrompt**: Write instructions for the AI character to stay in character.
   - **IMPORTANT**: {specificTopicInstruction}
   - Use phrases like: ""You are playing the role of [Name]...""
   - Instead of ""NEVER admit"", use: ""Maintain the immersion of the roleplay.""
   - Instead of ""REFUSE questions"", use: ""If the conversation drifts to unrelated topics, politely guide it back to the scene context.""
3. **FirstMessage**: An opening line in **{context.Language}**.

# STRICT CONSTRAINTS
- Vocabulary level: {context.DifficultyLevel} ({languageConstraints}).
- Do NOT use Vietnamese (unless it is the target language).

# OUTPUT JSON:
{{
  ""scenarioDescription"": ""..."",
  ""aiRole"": ""..."",
  ""systemPrompt"": ""Act as [Name], a [Role] at [Location]. Your goal is to help the user practice {context.Topic}. Stay in character. If the user asks out-of-context questions, remind them of the current setting politely."",
  ""firstMessage"": ""..."",
  ""tasks"": [ {{ ""taskDescription"": ""..."" }} ]
}}
";

            var json = await ChatAsync(context.MasterPrompt, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 1000, temperature: 0.8);

            try
            {
                return JsonSerializer.Deserialize<GeneratedConversationContentDto>(json, _json) ?? new GeneratedConversationContentDto();
            }
            catch
            {
                return new GeneratedConversationContentDto();
            }
        }


        public async Task<RoleplayResponseDto> GenerateResponseAsync(
             string systemPrompt,
             string userMessage,
             List<string> conversationHistory,
             string languageName = "English",
             string topic = "",
             string aiRoleName = "Friendly Teacher",
             string level = "A1",
             string scenarioContext = "")
        {
            // 1. Xử lý câu chào tạm biệt (Giữ nguyên logic cũ)
            var goodbyeKeywords = new[] { "bye", "goodbye", "see you", "tạm biệt", "hẹn gặp lại", "再见", "88" };
            if (goodbyeKeywords.Any(k => userMessage.ToLowerInvariant().Contains(k)))
            {
                string byeResponse = languageName.ToLower() switch
                {
                    var l when l.Contains("cn") || l.Contains("trung") || l.Contains("zh") => "再见！",
                    var l when l.Contains("vn") || l.Contains("việt") => "Tạm biệt nhé!",
                    _ => "Goodbye! See you!"
                };
                return new RoleplayResponseDto { Content = byeResponse, IsConversationFinished = true, IsOffTopic = false };
            }

            // 2. Tạo ngữ cảnh từ chối động (Dynamic Refusal Context)
            // Nếu topic rỗng, dùng default. Nếu có topic (ví dụ: Buying clothes), nó sẽ được chèn vào prompt.
            string safeTopic = string.IsNullOrWhiteSpace(topic) ? "our current activity" : topic;
            string safeLocation = string.IsNullOrWhiteSpace(scenarioContext) ? "this location" : scenarioContext;


            var guidancePrompt = $@"
### SIMULATION INSTRUCTIONS
You are simulating a conversation for a language learning app.
**Character Name**: {aiRoleName}
**Setting**: {safeLocation}
**Topic**: {safeTopic}
**Target Language**: {languageName} ({level})

### GUIDELINES FOR IMMERSION:
1. **Maintain Persona**: 
   - You are acting as {aiRoleName}. Respond exactly as this character would in real life.
   - If the user asks about your nature (e.g., ""Are you AI?""), simply reply in-character (e.g., ""I am {aiRoleName}, your shop assistant/colleague."") without breaking the flow.

2. **Stay Focused**:
   - Your knowledge is limited to what {aiRoleName} would know in {safeLocation}.
   - If the user asks encyclopedic questions or changes the subject (e.g., ""What is history?"", ""Do math""), treat it as a strange question in a real conversation.
   - politely suggest getting back to {safeTopic}.
   - Example refusal: ""I'm not sure about that, but shall we continue with {safeTopic}?""

3. **Contextual Awareness**:
   - Respond based on the physical surroundings of {safeLocation}.

### OUTPUT FORMAT:
User Input: ""{userMessage}""

Evaluate:
- If the input fits the roleplay -> Reply in {languageName}.
- If the input is completely unrelated -> Mark as OffTopic.

Output JSON:
{{
  ""content"": ""(Response in {languageName})"",
  ""isOffTopic"": true/false,
  ""isTaskCompleted"": false
}}
";

            // Gọi AI với Prompt mới
            var json = await ChatAsync(string.Empty, guidancePrompt, conversationHistory, jsonMode: true, maxTokens: 600, temperature: 0.7);

            try
            {
                var result = JsonSerializer.Deserialize<RoleplayResponseDto>(json, _json);
                return result ?? new RoleplayResponseDto { Content = "..." };
            }
            catch
            {
                // Fallback: Nếu JSON lỗi, gọi trực tiếp
                var content = await ChatAsync(guidancePrompt, userMessage, conversationHistory); // Dùng guidancePrompt thay vì systemPrompt cũ để đảm bảo luật
                return new RoleplayResponseDto { Content = content, IsOffTopic = false };
            }
        }

        // --- CÁC HÀM KHÁC GIỮ NGUYÊN ---

        private sealed class QuestionsWrapper { public List<VoiceAssessmentQuestion>? Questions { get; set; } }

        public async Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt, string targetLanguage)
        {
            string outputLang = targetLanguage;

            var enhancedPrompt = $@"{evaluationPrompt}

---
### CRITICAL OUTPUT INSTRUCTIONS:
1. **LANGUAGE**: Write all text analysis in **{outputLang}**.
2. **STRICT JSON STRUCTURE**:
You must return a valid JSON object matching EXACTLY this structure. Do not change property names.

{{
  ""overallScore"": 0,
  ""fluentAnalysis"": {{
    ""skillName"": ""Fluency"",
    ""qualitativeAssessment"": ""Detailed analysis here"",
    ""specificExamples"": [""example 1"", ""example 2""],
    ""suggestedImprovements"": [""advice 1"", ""advice 2""],
    ""currentLevel"": ""Beginner/Intermediate/Advanced""
  }},
  ""grammarAnalysis"": {{
    ""skillName"": ""Grammar"",
    ""qualitativeAssessment"": ""..."",
    ""specificExamples"": [],
    ""suggestedImprovements"": [],
    ""currentLevel"": ""...""
  }},
  ""vocabularyAnalysis"": {{
    ""skillName"": ""Vocabulary"",
    ""qualitativeAssessment"": ""..."",
    ""specificExamples"": [],
    ""suggestedImprovements"": [],
    ""currentLevel"": ""...""
  }},
  ""culturalAnalysis"": {{
    ""skillName"": ""Culture"",
    ""qualitativeAssessment"": ""..."",
    ""specificExamples"": [],
    ""suggestedImprovements"": [],
    ""currentLevel"": ""...""
  }},
  ""specificObservations"": [
    {{
      ""category"": ""Engagement/Grammar/Tone"",
      ""observation"": ""What happened"",
      ""impact"": ""Why it matters"",
      ""example"": ""Quote from user""
    }}
  ],
  ""positivePatterns"": [""What user did well""],
  ""areasNeedingWork"": [""What needs improvement""],
  ""progressSummary"": ""Overall summary""
}}
IMPORTANT: `specificObservations` MUST be an array of OBJECTS (with category, observation, impact, example), NOT strings.
";
            var json = await ChatAsync(string.Empty, enhancedPrompt, Array.Empty<string>(), jsonMode: true, maxTokens: 2500, temperature: 0.3);

            try
            {
                var result = JsonSerializer.Deserialize<ConversationEvaluationResult>(json, _json);
                if (result != null)
                {
                    if (result.OverallScore > 0)
                    {
                        if (result.FluentScore == 0) result.FluentScore = result.OverallScore;
                        if (result.GrammarScore == 0) result.GrammarScore = result.OverallScore;
                        if (result.VocabularyScore == 0) result.VocabularyScore = result.OverallScore;
                        if (result.CulturalScore == 0) result.CulturalScore = result.OverallScore;
                    }
                    return result;
                }
                return new ConversationEvaluationResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse detailed evaluation. JSON: {Json}", json);
                return new ConversationEvaluationResult
                {
                    AIFeedback = "AI Error: Could not parse evaluation results.",
                    ProgressSummary = "Evaluation failed due to format error."
                };
            }
        }

        public async Task<SynonymSuggestionDto> GenerateSynonymSuggestionsAsync(string userMessage, string targetLanguage, string currentLevel)
        {
            try
            {
                _logger.LogInformation("AzureOpenAI: Generating synonyms for '{Message}' in {Language} at {Level}",
                    userMessage, targetLanguage, currentLevel);

                var nextLevel = GetNextLevel(currentLevel);
                var isMaxLevel = (currentLevel == nextLevel);
                var levelGuidance = isMaxLevel
                    ? $"at the current {currentLevel} level with more sophisticated/native-like expressions"
                    : $"at the NEXT proficiency level only ({nextLevel})";
                var levelRules = GetStrictLanguageConstraints(targetLanguage, nextLevel);

                var prompt = $@"
CONTEXT:
- User is learning: {targetLanguage} (Target Language).
- User's Current Level: {currentLevel}.
- User Input: ""{userMessage}""

TASK:
Provide 2-3 better, more natural, or more sophisticated expressions for the User Input strictly IN {targetLanguage}.
CRITICAL LEVEL RULES for suggestions:
{levelRules}
(The `alternativeText` must strictly follow these rules. Do not suggest words that are too difficult for {nextLevel}).

CRITICAL RULES:
1. **Output Language**: The `alternativeText` field MUST ALWAYS be in {targetLanguage}. 
   - IF User Input is in Vietnamese/English: Translate the MEANING to {targetLanguage} first, then provide improved versions in {targetLanguage}.
   - IF User Input is in {targetLanguage}: Improve it directly in {targetLanguage}.
2. **Explanation Language**: The `difference` and `explanation` fields MUST be in VIETNAMESE (so the learner understands why it's better).
3. **Level**: Suggestions should be {levelGuidance}.

JSON OUTPUT FORMAT:
{{
  ""originalMessage"": ""{userMessage}"",
  ""currentLevel"": ""{currentLevel}"",
  ""alternatives"": [
    {{
      ""level"": ""{nextLevel}"",
      ""alternativeText"": ""[SUGGESTION IN {targetLanguage}]"", 
      ""difference"": ""[GIẢI THÍCH NGẮN GỌN BẰNG TIẾNG VIỆT]"",
      ""exampleUsage"": ""[EXAMPLE IN {targetLanguage}]""
    }}
  ],
  ""explanation"": ""[TỔNG QUAN BẰNG TIẾNG VIỆT]""
}}
Return valid JSON only.";

                var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 800, temperature: 0.4);

                var result = !string.IsNullOrWhiteSpace(json)
                    ? JsonSerializer.Deserialize<SynonymSuggestionDto>(json, _json)
                    : null;

                if (result != null)
                {
                    return result;
                }
                else
                {
                    return new SynonymSuggestionDto
                    {
                        OriginalMessage = userMessage,
                        CurrentLevel = currentLevel,
                        Alternatives = new()
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AzureOpenAI: Failed to generate synonym suggestions");
                return new SynonymSuggestionDto
                {
                    OriginalMessage = userMessage,
                    CurrentLevel = currentLevel,
                    Alternatives = new(),
                    Explanation = "Unable to generate suggestions at this time."
                };
            }
        }

        private string GetNextLevel(string currentLevel)
        {
            var levelMap = new Dictionary<string, string>
            {
                { "A1", "A2" }, { "A2", "B1" }, { "B1", "B2" }, { "B2", "C1" }, { "C1", "C2" }, { "C2", "C2" },
                { "N5", "N4" }, { "N4", "N3" }, { "N3", "N2" }, { "N2", "N1" }, { "N1", "N1" },
                { "HSK1", "HSK2" }, { "HSK2", "HSK3" }, { "HSK3", "HSK4" }, { "HSK4", "HSK5" }, { "HSK5", "HSK5" }
            };

            return levelMap.TryGetValue(currentLevel, out var next) ? next : currentLevel;
        }

        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            var prompt = $"Translate the following text from {sourceLanguage} to {targetLanguage}. Only the translation.\n{text}";
            return await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: false);
        }

        public async Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(string languageCode, string languageName, string? programName = null)
        {
            string[] labels = languageCode?.ToLowerInvariant().StartsWith("ja") == true
                ? new[] { "N5", "N4", "N3", "N2" }
                : languageCode?.ToLowerInvariant().StartsWith("zh") == true
                ? new[] { "HSK1", "HSK2", "HSK3", "HSK4" }
                : new[] { "A1", "A2", "B1", "B2" };

            string commonBan = "Do not use generic templates like 'Introduce yourself', 'Daily routine', 'Recent experience', 'Give an opinion', or 'about technology'. Do NOT include phrases like 'in English'/'in Japanese' in the question text. Write directly in the target language. Avoid vague, off-topic questions like \"What is that, Where is this?";
            string lengths = "Enforce lengths: Q1 must be 2-3 words; Q2 must be 3-5 words; Q3 must be 6-8 words (short phrase); Q4 must be 8-12 words (short sentence).";
            string constraints = $"Return a JSON object with property 'questions' (array of exactly 4). Each is a concise speaking prompt for {languageName} ({languageCode}), tailored to program '{programName}', with ascending difficulty strictly set to [{string.Join(',', labels)}] in order. {lengths} {commonBan}";
            string fields = "Each item fields: questionNumber (1..4), question, promptText (<=15 words), vietnameseTranslation, wordGuides (nullable), questionType='speaking', difficulty (one of the ordered labels), maxRecordingSeconds = 30/60/90/120. Return JSON only.";
            var prompt1 = $"{constraints}\n{fields}";
            var json = await ChatAsync(string.Empty, prompt1, Array.Empty<string>(), jsonMode: true, maxTokens: 640, temperature: 0.25);

            var list = TryParseQuestions(json);
            list = SanitizeQuestions(list, languageName);
            if (IsLowDiversity(list))
            {
                var prompt2 = $"{constraints} Use practical, real-life contexts for '{programName}' (e.g., meetings, pitches, negotiations, customer service, travel). Avoid duplicates. {fields}";
                var json2 = await ChatAsync(string.Empty, prompt2, Array.Empty<string>(), jsonMode: true, maxTokens: 680, temperature: 0.35);
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
            return similar >= 2;
        }

        public async Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(List<VoiceAssessmentQuestion> questions, string languageCode, string languageName, List<string>? programLevelNames = null)
        {
            var scale = programLevelNames != null && programLevelNames.Any()
                ? $"[{string.Join(", ", programLevelNames)}]"
                : "CEFR/HSK/JLPT";

            var qData = questions.Select(q => new
            {
                q.QuestionNumber,
                q.Difficulty,
                q.PromptText,
                q.IsSkipped,
                Transcript = q.Transcript ?? "(empty)"
            }).ToList();

            var qJson = JsonSerializer.Serialize(qData, new JsonSerializerOptions { WriteIndented = false });

            var prompt = $@"You are evaluating {languageName} speaking responses based on transcripts only (no audio).
Return a JSON object with:
- overallLevel: string from {scale}
- overallScore: number 0-100
- questionResults: array with {questions.Count} items, each has questionNumber, spokenWords[], missingWords[], accuracyScore, pronunciationScore, fluencyScore, grammarScore, feedback
- strengths: array of strings
- weaknesses: array of strings
- recommendedCourses: array (can be empty)

Questions data:
{qJson}

Rules:
1. If transcript is empty or '(empty)', give low scores (20-40) and feedback='No valid transcript'.
2. overallLevel MUST be one of {scale}.
3. Return valid JSON only, no markdown.";

            var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 1200, temperature: 0.3);

            try
            {
                var result = JsonSerializer.Deserialize<BatchVoiceEvaluationResult>(json, _json);
                if (result == null || string.IsNullOrWhiteSpace(result.OverallLevel))
                {
                    return CreateFallbackBatch(questions, programLevelNames?.FirstOrDefault() ?? "Beginner");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JSON parse failed for batch eval");
                return CreateFallbackBatch(questions, programLevelNames?.FirstOrDefault() ?? "Beginner");
            }
        }

        private BatchVoiceEvaluationResult CreateFallbackBatch(List<VoiceAssessmentQuestion> questions, string defaultLevel)
        {
            return new BatchVoiceEvaluationResult
            {
                OverallLevel = defaultLevel,
                OverallScore = 50,
                QuestionResults = questions.Select(q => new QuestionEvaluationResult
                {
                    QuestionNumber = q.QuestionNumber,
                    Feedback = q.IsSkipped ? "Skipped" : "Unable to evaluate - AI error",
                    AccuracyScore = 50,
                    PronunciationScore = 50,
                    FluencyScore = 50,
                    GrammarScore = 50
                }).ToList(),
                Strengths = new List<string> { "Completed assessment" },
                Weaknesses = new List<string> { "AI evaluation unavailable" },
                RecommendedCourses = new List<CourseRecommendation>()
            };
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
            var prompt = $"Recommend 3-5 courses for the user below. Return JSON AiCourseRecommendationDto.\n{JsonSerializer.Serialize(payload, _json)}";
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
            var prompt = $"Return 8-10 study tips as a plain text list (one per line) for this learner: {JsonSerializer.Serialize(survey, _json)}";
            var text = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: false, maxTokens: 300, temperature: 0.3);
            return text.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select(s => s.TrimStart('-', ' ', '•').Trim()).ToList();
        }

        public async Task<TeacherQualificationAnalysisDto> AnalyzeTeacherQualificationsAsync(TeacherApplicationDto application, List<TeacherCredentialDto> credentials)
        {
            var prompt = $"Analyze teacher qualifications. Return JSON TeacherQualificationAnalysisDto.\nApplication: {JsonSerializer.Serialize(application, _json)}\nCredentials: {JsonSerializer.Serialize(credentials, _json)}";
            var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 700, temperature: 0.2);
            try { return JsonSerializer.Deserialize<TeacherQualificationAnalysisDto>(json, _json) ?? new TeacherQualificationAnalysisDto(); } catch { return new TeacherQualificationAnalysisDto(); }
        }

        private string GetStrictLanguageConstraints(string language, string level)
        {
            language = language.ToLower();
            level = level.ToUpper();

            if (language.Contains("chin") || language.Contains("zh") || language.Contains("trung"))
            {
                return level switch
                {
                    "HSK1" or "A1" => "Use ONLY very basic words (HSK1). Short sentences. Pinyin is optional but focus on simple Hanzi.",
                    "HSK2" or "A2" => "Simple daily conversation. Avoid idioms (Chengyu).",
                    _ => "Natural native expression."
                };
            }

            if (language.Contains("eng") || language.Contains("anh"))
            {
                return level switch
                {
                    "A1" => "Use basic Subject-Verb-Object sentences. Common words only (Top 500). No slang.",
                    "A2" => "Simple past/future tense allowed. Conversational but clear.",
                    "B1" or "B2" => "Business casual allowed, more complex grammar.",
                    _ => "Fluent and sophisticated."
                };
            }

            return "Speak naturally but adjust complexity to the user's level.";
        }

        public async IAsyncEnumerable<string> GenerateResponseStreamAsync(
            string systemPrompt,
            string userMessage,
            List<string> history)
        {
            var deploymentName = !string.IsNullOrWhiteSpace(_settings.ChatDeployment)
                ? _settings.ChatDeployment
                : "gpt-4o-mini";

            var chatClient = _openAiClient.GetChatClient(deploymentName);

            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                new SystemChatMessage(systemPrompt)
            };

            foreach (var hist in history)
            {
                if (hist.StartsWith("User:"))
                    messages.Add(new UserChatMessage(hist.Substring(5).Trim()));
                else if (hist.StartsWith("AI:"))
                    messages.Add(new AssistantChatMessage(hist.Substring(3).Trim()));
            }

            messages.Add(new OpenAI.Chat.UserChatMessage(userMessage));

            var options = new OpenAI.Chat.ChatCompletionOptions
            {
                Temperature = 0.7f,
                MaxOutputTokenCount = 300
            };

            var completionUpdates = chatClient.CompleteChatStreamingAsync(messages, options);

            await foreach (var update in completionUpdates)
            {
                foreach (var contentPart in update.ContentUpdate)
                {
                    if (!string.IsNullOrEmpty(contentPart.Text))
                    {
                        yield return contentPart.Text;
                    }
                }
            }
        }
    }
}