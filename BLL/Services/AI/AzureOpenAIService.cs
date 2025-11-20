using Azure;
using Azure.AI.OpenAI;
using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Assement;
using Common.DTO.Conversation; // Đảm bảo RoleplayResponseDto nằm trong namespace này
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


        public async Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context)
        {
            var languageConstraints = GetStrictLanguageConstraints(context.Language, context.DifficultyLevel);
            var rubric = BuildDifficultyRubric(context.DifficultyLevel);

            // Random vibe (Giữ nguyên logic random nhưng sửa text cho nhẹ nhàng hơn)
            var vibes = new[]
            {
        "Urgent and rushed",
        "Relaxed and cozy",
        "A minor misunderstanding",
        "Pleasantly surprised",
        "Seeking advice",
        "Celebratory mood",
        "Slightly confused",
        "Professional and formal",
        "Busy environment",
        "Quiet and secretive"
    };
            var selectedVibe = vibes[Random.Shared.Next(vibes.Length)];

            // --- FIX: PROMPT AN TOÀN HƠN ---
            var prompt = $@"# Generate Roleplay Scenario

CONTEXT:
- Topic: {context.Topic}
- Level: {context.DifficultyLevel}
- Tone: {selectedVibe}

CRITICAL LANGUAGE CONSTRAINTS:
{languageConstraints}

INSTRUCTIONS:
...
## 3. CHARACTER GUIDELINES (System Prompt):
- Embody the character '{selectedVibe}'.
- **IMPORTANT**: You must speak strictly according to: {languageConstraints}
- If A1/N5/HSK1: Use very short, simple sentences. Be patient.
...

## 1. SCENARIO DETAILS (in {context.Language}):
- Describe the scene, fitting the '{selectedVibe}' tone.
- Include sensory details (sound, lighting, etc.).
- Explain the immediate reason for the conversation.

## 2. CHARACTER IDENTITY (in {context.Language}):
- Define a character matching the tone.
- Format: ""[Name], [Role] ([Personality])""

## 3. CHARACTER GUIDELINES (Internal Instructions):
- The AI should embody the character and the '{selectedVibe}' tone.
- Language: {context.Language} only.
- Length: Keep replies concise (1-2 sentences).

## 4. OPENING LINE (in {context.Language}):
- Start directly in the situation. Avoid generic greetings like ""Hello"" if they don't fit the urgency.
- Demonstrate the emotion immediately.

## 5. TASKS (3 items):
- Task 1 (Easy) -> Task 2 (Medium) -> Task 3 (Hard).
- Specific to this scenario.

## JSON OUTPUT FORMAT:
{{
  ""scenarioDescription"": ""..."",
  ""aiRole"": ""..."",
  ""systemPrompt"": ""..."",
  ""firstMessage"": ""..."",
  ""tasks"": [ {{""taskDescription"": ""...""}}, ... ]
}}";

           
            var json = await ChatAsync(context.MasterPrompt, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 800, temperature: 0.8);

            try
            {
                return JsonSerializer.Deserialize<GeneratedConversationContentDto>(json, _json) ?? new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice {context.Topic}",
                    AIRole = "Partner",
                    SystemPrompt = context.MasterPrompt,
                    FirstMessage = "Hello.",
                    Tasks = new List<ConversationTaskDto>()
                };
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
    string aiRoleName = "AI Partner",
    string level = "A1") 
        {

            var levelConstraints = GetStrictLanguageConstraints(languageName, level);
            var goodbyeKeywords = new[] { "bye", "goodbye", "see you", "tạm biệt", "さようなら", "再见", "zàijiàn", "sayonara" };
            var userLower = userMessage.ToLowerInvariant();

            if (goodbyeKeywords.Any(k => userLower.Contains(k)))
            {
              
                var langInput = languageName?.ToLowerInvariant()?.Trim() ?? "";

                string byeResponse;

              
                if (langInput.Contains("japan") || langInput.Contains("jp") || langInput.Contains("nhật") || langInput.Contains("ja"))
                {
                    byeResponse = "さようなら！良い一日を！"; // Tiếng Nhật
                }
                else if (langInput.Contains("chin") || langInput.Contains("zh") || langInput.Contains("trung") || langInput.Contains("cn"))
                {
                    byeResponse = "再见！祝你有美好的一天！"; // Tiếng Trung
                }
                else if (langInput.Contains("english") || langInput.Contains("en") || langInput.Contains("anh") || langInput.Contains("us") || langInput.Contains("uk"))
                {
                    byeResponse = "Goodbye! Have a great day!"; // Tiếng Anh
                }
                else
                {
                    byeResponse = "Tạm biệt! Chúc bạn một ngày tốt lành!"; // Mặc định (Tiếng Việt)
                }

                return new RoleplayResponseDto
                {
                    Content = byeResponse,
                    IsConversationFinished = true,
                    IsOffTopic = false
                };
            }

            // 2. Build Guidance & Roleplay Prompt
            // Kỹ thuật: Yêu cầu AI kiểm tra Condition trước khi trả lời.
            // Nếu lạc đề -> Trả về lời nhắc (Tiếng Việt). Nếu đúng -> Trả về hội thoại (Target Language).
            var guidancePrompt = $@"
{systemPrompt}

---
### UPDATE FOR NEXT RESPONSE:
User Input: ""{userMessage}""
Context: Topic '{topic}', Role '{aiRoleName}', Level '{level}'.

**STRICT LANGUAGE ENFORCEMENT:**
{levelConstraints}
(Ensure your response strictly matches this complexity level. Do not use words/grammar above this level).

Please respond using ONE of these modes:

MODE A: GUIDANCE (If user is off-topic/confused)
- JSON `isOffTopic: true`.
- Content (Vietnamese): ""Có thể bạn đang hơi lạc đề. Tôi là {aiRoleName}. Với trình độ {level}, bạn nên nói: '[Suggest sentence strictly complying with {levelConstraints}]'""

MODE B: ROLEPLAY (Normal conversation)
- JSON `isOffTopic: false`.
- Content ({languageName}): Your in-character reply. MUST follow: {levelConstraints}.

### REQUIRED JSON OUTPUT:
{{
  ""content"": ""..."",
  ""isOffTopic"": true/false,
  ""isTaskCompleted"": true/false
}}
";

            _logger.LogInformation("AzureOpenAI: Prompting with Safe Guidance Logic");

            // Giảm Temperature xuống 0.3 hoặc 0.4 để AI tuân thủ luật JSON tốt hơn
            var json = await ChatAsync(string.Empty, guidancePrompt, conversationHistory, jsonMode: true, maxTokens: 500, temperature: 0.4);

            try
            {
                var result = JsonSerializer.Deserialize<RoleplayResponseDto>(json, _json);
                return result ?? new RoleplayResponseDto { Content = "..." };
            }
            catch (Exception ex)
            {
                // Fallback
                var content = await ChatAsync(systemPrompt, userMessage, conversationHistory);
                return new RoleplayResponseDto { Content = content, IsOffTopic = false };
            }
        }

        // UPDATED: Đánh giá chi tiết hơn, không set điểm cứng
        public async Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt, string targetLanguage)
        {
            string outputLang = targetLanguage;

            // FIX: Cung cấp JSON Skeleton cụ thể để AI không trả về sai cấu trúc
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
                // =================================================================================
                // FIX: CẬP NHẬT PROMPT ĐỂ ÉP NGÔN NGỮ ĐÍCH
                // =================================================================================
                var prompt = $@"
CONTEXT:
- User is learning: {targetLanguage} (Target Language).
- User's Current Level: {currentLevel}.
- User Input: ""{userMessage}""

TASK:
Provide 2-3 better, more natural, or more sophisticated expressions for the User Input strictly IN {targetLanguage}.
CRITICAL LEVEL RULES for suggestions:
{{levelRules}}
(The `alternativeText` must strictly follow these rules. Do not suggest words that are too difficult for {{nextLevel}}).
...
"";
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

                // Gọi AI
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
            // difficulty labels per language
            string[] labels = languageCode?.ToLowerInvariant().StartsWith("ja") == true
                ? new[] { "N5", "N4", "N3", "N2" }
                : languageCode?.ToLowerInvariant().StartsWith("zh") == true
                ? new[] { "HSK1", "HSK2", "HSK3", "HSK4" }
                : new[] { "A1", "A2", "B1", "B2" };

            string commonBan = "Do not use generic templates like 'Introduce yourself', 'Daily routine', 'Recent experience', 'Give an opinion', or 'about technology'. Do NOT include phrases like 'in English'/'in Japanese' in the question text. Write directly in the target language. Avoid vague, off-topic questions like \"What is that, Where is this?";
            string lengths = "Enforce lengths: Q1 must be2-3 words; Q2 must be3-5 words; Q3 must be6-8 words (short phrase); Q4 must be8-12 words (short sentence).";
            string constraints = $"Return a JSON object with property 'questions' (array of exactly4). Each is a concise speaking prompt for {languageName} ({languageCode}), tailored to program '{programName}', with ascending difficulty strictly set to [{string.Join(',', labels)}] in order. {lengths} {commonBan}";
            string fields = "Each item fields: questionNumber (1..4), question, promptText (<=15 words), vietnameseTranslation, wordGuides (nullable), questionType='speaking', difficulty (one of the ordered labels), maxRecordingSeconds =30/60/90/120. Return JSON only.";
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
        private static string GetStrictLanguageConstraints(string language, string level)
        {
            var lang = language?.ToLowerInvariant() ?? "";
            var lvl = level?.ToUpperInvariant() ?? "";

            // --- TIẾNG NHẬT (JLPT) ---
            if (lang.Contains("japan") || lang.Contains("jp") || lang.Contains("nhật"))
            {
                return lvl switch
                {
                    "N5" => "STRICT N5 RULES: Use ONLY basic vocabulary (approx 800 words). Use polite forms (Desu/Masu). Avoid complex Kanji (use Hiragana/Katakana primarily). Simple sentences (Subject-Object-Verb). NO casual/slang.",
                    "N4" => "STRICT N4 RULES: Basic conjunctions (kara, node). Simple compound sentences. Polite forms mainly, but can introduce some plain forms if context fits. Kanji limited to N4 list.",
                    "N3" => "STRICT N3 RULES: Everyday conversation speed. Introduction of specific grammatical structures (koto ga aru, tsumori). Mix of polite and plain forms appropriate for the role.",
                    "N2" => "STRICT N2 RULES: Business/Formal level. Use Keigo (Honorifics) if role requires. Complex sentence structures and abstract topics.",
                    "N1" => "STRICT N1 RULES: Native level. Idioms, nuanced expressions, advanced vocabulary. Full natural speed and complexity.",
                    _ => "Adjust simple polite Japanese."
                };
            }

            // --- TIẾNG TRUNG (HSK) ---
            if (lang.Contains("chin") || lang.Contains("zh") || lang.Contains("trung") || lang.Contains("cn"))
            {
                return lvl switch
                {
                    "HSK1" => "STRICT HSK1 RULES: Use ONLY the 150 basic HSK1 words. Very short sentences (3-6 words). No complex grammar. Pinyin support if possible (but output characters).",
                    "HSK2" => "STRICT HSK2 RULES: HSK2 vocabulary (300 words). Simple daily exchanges. Basic questions and answers.",
                    "HSK3" => "STRICT HSK3 RULES: HSK3 vocabulary (600 words). Connected paragraphs. Daily life topics.",
                    "HSK4" => "STRICT HSK4 RULES: HSK4 vocabulary (1200 words). Discuss abstract topics moderately. Complex grammar allowed.",
                    "HSK5" => "STRICT HSK5 RULES: Formal speech, newspapers, movies. Full fluency.",
                    "HSK6" => "STRICT HSK6 RULES: Native level literary and technical proficiency.",
                    _ => "Simple Chinese."
                };
            }

            // --- TIẾNG ANH (CEFR) ---
            // Mặc định là Tiếng Anh nếu không khớp trên
            return lvl switch
            {
                var x when x.Contains("A1") => "STRICT A1 RULES: Use top 500 most common words ONLY. Present Simple & Present Continuous tenses mainly. Short sentences (max 8 words). NO idioms. NO passive voice. Speak slowly and clearly text-wise.",
                var x when x.Contains("A2") => "STRICT A2 RULES: Top 1000 common words. Past Simple, Future with 'going to'. Simple descriptions. Connectors like 'and', 'but', 'because'.",
                var x when x.Contains("B1") => "STRICT B1 RULES: Standard English. Present Perfect, Conditionals (Type 1). Can express opinions and reasons.",
                var x when x.Contains("B2") => "STRICT B2 RULES: Fluency and spontaneity. Complex arguments. Phrasal verbs allowed.",
                var x when x.Contains("C1") => "STRICT C1 RULES: Advanced vocabulary, idiomatic expressions, flexible sentence structure.",
                var x when x.Contains("C2") => "STRICT C2 RULES: Native proficiency, subtle nuances, cultural references.",
                _ => "Simple English."
            };
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

