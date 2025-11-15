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

            var prompt = $@"# Generate IMMERSIVE Roleplay Scenario for {context.Language}

Create a VIVID, REALISTIC roleplay scenario (NOT a generic description).

IMPORTANT VARIETY RULES:
- Every time you answer, create a CLEARLY DIFFERENT scenario.
- Do NOT reuse the same structure, details, or rhythm as the examples.
- Do NOT copy any sentences, phrases, or patterns from the examples.
- Vary time of day, weekday/weekend, location type, relationship between characters, and stakes.
- Frequently change sentence structure to avoid feeling repetitive.

Base the scenario tightly on:
- Topic: {context.Topic}
- Difficulty: {context.DifficultyLevel} — {rubric}
- Guidelines: {context.ScenarioGuidelines}
- Roleplay notes: {context.RoleplayInstructions}
- Evaluation focus: {context.EvaluationCriteria}

## SCENARIO DESCRIPTION (25-50 words in {context.Language}):
Must include:
- Exact time & specific place: ""Monday 9 AM at Starbucks 5th Ave"" NOT ""at a cafe""
- Character name, age, brief appearance
- What JUST happened to trigger this conversation
- Current emotion/urgency
- At least ONE sensory detail (weather, sound, atmosphere, smell, light, etc.)
- Wide variety of characters, roles, and times across different calls so users don't get repeated patterns.
- Vary sentence structure heavily (avoid always starting with time, or ""You are..."").

Examples below are ONLY to show level of detail and style. 
Do NOT copy their structure, order of information, or wording.

GOOD example (for level of detail ONLY):
""It's 2 PM, raining. You're Emma, 25, sitting in the HR office at Tech Solutions. The interviewer just asked about your previous job failure. Your coffee is cold. This interview could change everything.""

BAD example:
""You are having a job interview to discuss your experience.""

Forbidden patterns:
- Do NOT start with ""It's 2 PM, raining."" or any slight variation.
- Do NOT end with sentences like ""This X could change everything.""
- Do NOT always use ""You're [name], [age], sitting in..."". Change it.

## AI ROLE (in {context.Language}, max 40 chars):
Format: ""[Name], [Role] ([personality trait])""
Example: ""Mr. Chen, Hiring Manager (严肃但公正)"" or ""Sophie, Server (friendly but busy)""
Vary names, roles, and traits across scenarios.

## SYSTEM PROMPT:
Create detailed character instructions:
- Full character identity and personality
- Speak AS the character, never describe from outside
- Reply ONLY in {context.Language}, no other languages
- Use natural conversational tone for {context.DifficultyLevel} level
- Keep responses 1-2 sentences, realistic dialogue
- Show emotion through word choice
- Reference specific scenario details
- Stay on topic: {context.Topic}
- Gently redirect if user goes off-topic, staying in character
- NO emojis, NO markdown, NO role labels like ""AI:"" or ""Character:""

## FIRST MESSAGE (in {context.Language}):
MUST be spoken IN-CHARACTER as direct dialogue, NOT a greeting template.
- Include brief action/emotion in *asterisks* if needed
- Reference something specific from scenario
- Ask a question or create situation requiring response
- Show personality immediately
- Do NOT reuse any of the example sentences below.

GOOD examples (for style ONLY, do NOT copy):
- ""*glances at your resume* I see a gap here between March and July. What happened?""
- ""*approaches your table with notepad* I'm so sorry for the wait! Ready to order?""
- ""*looks up from computer* Your flight number? I'll search our system right away.""

BAD examples:
- ""Hello! Let's start our conversation.""
- ""Hi there! I'm excited to discuss {context.Topic} with you.""
- ""Welcome! How can I help you today?"" (too generic)

## TASKS (exactly 3):
Progressive difficulty for {context.DifficultyLevel} level.
Each task: one specific action, max 80 chars, imperative form.
Task 1 (easy) → Task 2 (medium) → Task 3 (challenging)

Examples for {context.Topic}:
Task 1: ""Greet and explain your situation clearly""
Task 2: ""Ask specific questions about the resolution process""
Task 3: ""Negotiate a satisfactory solution professionally""

## RETURN STRICT JSON FORMAT:
{{
  ""scenarioDescription"": ""[detailed scenario in {context.Language}]"",
  ""aiRole"": ""[Name, Role (trait)]"",
  ""systemPrompt"": ""[character instructions as described above]"",
  ""firstMessage"": ""[in-character dialogue in {context.Language}]"",
  ""tasks"": [
    {{""taskDescription"": ""[task 1 in {context.Language}]""}},
    {{""taskDescription"": ""[task 2 in {context.Language}]""}},
    {{""taskDescription"": ""[task 3 in {context.Language}]""}}
  ]
}}

Return ONLY valid JSON, no markdown blocks, no extra text.";

            var json = await ChatAsync(context.MasterPrompt, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 800, temperature: 0.6);
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

        // UPDATED: Đánh giá chi tiết hơn, không set điểm cứng
        public async Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt)
        {
            // Cải thiện prompt để yêu cầu phân tích chi tiết
            var enhancedPrompt = $@"{evaluationPrompt}

CRITICAL: Provide a DETAILED, QUALITATIVE analysis. Don't just give scores.
Return JSON with:
- overallScore (0-100, for compatibility)
- fluentAnalysis, grammarAnalysis, vocabularyAnalysis, culturalAnalysis (each with detailed object containing:
  * skillName: name of the skill
  * qualitativeAssessment: detailed narrative description
  * specificExamples: array of concrete examples from conversation
  * suggestedImprovements: array of actionable advice
  * currentLevel: ""Beginner""/""Intermediate""/""Advanced"")
- specificObservations: array of objects with category, observation, impact, example
- positivePatterns: array of strings (what user did well)
- areasNeedingWork: array of strings (specific things to improve)
- progressSummary: overall narrative assessment

For each skill analysis, provide concrete examples and practical suggestions.
Return valid JSON only, no markdown.";

            var json = await ChatAsync(string.Empty, enhancedPrompt, Array.Empty<string>(), jsonMode: true, maxTokens: 1500, temperature: 0.3);
            try
            {
                var result = JsonSerializer.Deserialize<ConversationEvaluationResult>(json, _json);
                if (result != null)
                {
                    // Ensure backwards compatibility: nếu có detailed analysis thì tính điểm từ đó
                    if (result.FluentAnalysis != null || result.GrammarAnalysis != null)
                    {
                        result.FluentScore = result.OverallScore * 0.9f;
                        result.GrammarScore = result.OverallScore * 0.85f;
                        result.VocabularyScore = result.OverallScore * 0.95f;
                        result.CulturalScore = result.OverallScore * 0.8f;
                    }
                    return result;
                }
                return new ConversationEvaluationResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse detailed evaluation");
                return new ConversationEvaluationResult { AIFeedback = json };
            }
        }

        // NEW: Generate synonym suggestions
        public async Task<SynonymSuggestionDto> GenerateSynonymSuggestionsAsync(string userMessage, string targetLanguage, string currentLevel)
        {
            try
            {
                _logger.LogInformation("AzureOpenAI: Generating synonyms for '{Message}' in {Language} at {Level}", 
                    userMessage, targetLanguage, currentLevel);
                
                // Determine next level only
                var nextLevel = GetNextLevel(currentLevel);
                _logger.LogInformation("AzureOpenAI: Current level={Current}, Next level={Next}", currentLevel, nextLevel);
                
                // Special handling for max levels
                var isMaxLevel = (currentLevel == nextLevel);
                var levelGuidance = isMaxLevel 
                    ? $"at the current {currentLevel} level with more sophisticated/native-like expressions"
                    : $"at the NEXT proficiency level only ({nextLevel})";
                
                _logger.LogInformation("AzureOpenAI: IsMaxLevel={IsMax}, LevelGuidance={Guidance}", isMaxLevel, levelGuidance);
                
                var prompt = $@"User said: ""{userMessage}"" in {targetLanguage} (current level: {currentLevel})

Provide 2-3 BETTER alternatives {levelGuidance}.
Do NOT suggest multiple levels - focus on natural progression.

{(isMaxLevel ? 
$@"Since user is at {currentLevel} (highest level), suggest:
- More native-like expressions
- More sophisticated vocabulary
- More idiomatic phrases
All at {currentLevel} level." :
$@"Example:
If user (A2) said: ""I want to buy this""
Suggest (B1): ""I would like to purchase this"" or ""I'd like to buy this item""
NOT B2/C1/C2 - only the NEXT level!")}

Return JSON:
{{
  ""originalMessage"": ""{userMessage}"",
  ""currentLevel"": ""{currentLevel}"",
  ""alternatives"": [
    {{
      ""level"": ""{nextLevel}"",
      ""alternativeText"": ""better expression at {nextLevel}"",
      ""difference"": ""why this is more advanced"",
      ""exampleUsage"": ""example in context""
    }}
  ],
  ""explanation"": ""brief summary""
}}

Return 2-3 alternatives only, all at level {nextLevel}.";

                _logger.LogDebug("AzureOpenAI: Calling ChatAsync with prompt (first 200 chars): {Prompt}", 
                    prompt.Length > 200 ? prompt.Substring(0, 200) + "..." : prompt);

                var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 600, temperature: 0.4);
                
                _logger.LogInformation("AzureOpenAI: Received JSON response (length={Length})", json?.Length ?? 0);
                _logger.LogDebug("AzureOpenAI: JSON response: {Json}", json);
                
                var result = !string.IsNullOrWhiteSpace(json) 
                    ? JsonSerializer.Deserialize<SynonymSuggestionDto>(json, _json)
                    : null;
                
                if (result != null)
                {
                    _logger.LogInformation("AzureOpenAI: Successfully deserialized. OriginalMessage={Orig}, Alternatives count={Count}", 
                        result.OriginalMessage, result.Alternatives?.Count ?? 0);
                    return result;
                }
                else
                {
                    _logger.LogWarning("AzureOpenAI: Deserialization returned NULL");
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

            _logger.LogInformation("Batch eval prompt length: {Len} chars, maxTokens: 1200", prompt.Length);
   var json = await ChatAsync(string.Empty, prompt, Array.Empty<string>(), jsonMode: true, maxTokens: 1200, temperature: 0.3);
            _logger.LogInformation("AI raw response (first 300 chars): {Preview}", json.Length > 300 ? json.Substring(0, 300) : json);

       try 
            { 
     var result = JsonSerializer.Deserialize<BatchVoiceEvaluationResult>(json, _json);
                if (result == null || string.IsNullOrWhiteSpace(result.OverallLevel))
         {
     _logger.LogWarning("Parsed result is null or OverallLevel empty. Falling back.");
     return CreateFallbackBatch(questions, programLevelNames?.FirstOrDefault() ?? "Beginner");
                }
         return result;
         } 
            catch (Exception ex)
            {
         _logger.LogError(ex, "JSON parse failed. Raw: {Json}", json.Length > 500 ? json.Substring(0, 500) : json);
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
    }
}