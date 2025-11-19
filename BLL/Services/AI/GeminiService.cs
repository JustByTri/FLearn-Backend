//using BLL.IServices.AI;
//using BLL.Settings;
//using Common.DTO.Assement;
//using Common.DTO.Conversation;
//using Common.DTO.Learner;
//using Common.DTO.Teacher;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Logging;
//using Microsoft.Extensions.Options;
//using System.Net;
//using System.Net.Http.Json;
//using System.Text;
//using System.Text.Json;
//using System.Text.Json.Serialization;
//using System.Linq;

//namespace BLL.Services.AI
//{
//    public class GeminiService : IGeminiService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly GeminiSettings _settings;
//        private readonly ILogger<GeminiService> _logger;

//        public GeminiService(
//            HttpClient httpClient,
//            IOptions<GeminiSettings> settings,
//            ILogger<GeminiService> logger)
//        {
//            _httpClient = httpClient;
//            _settings = settings.Value;
//            _logger = logger;
//        }

//        // ============ VOICE ASSESSMENT METHODS ============

//        /// <summary>
//        /// ✅ Tạo câu hỏi theo ngôn ngữ + chương trình
//        /// </summary>
//        public async Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(
//            string languageCode,
//            string languageName,
//            string? programName = null)
//        {
//            try
//            {
//                _logger.LogInformation("Đang tạo câu hỏi assessment cho {LanguageCode}, Program: {ProgramName}",
//                    languageCode, programName ?? "General");

//                // 1. Tạo prompt
//                var prompt = BuildVoiceAssessmentQuestionsPrompt(languageCode, languageName, programName);

//                // 2. Gọi API
//                var response = await CallGeminiApiAsync(prompt);

//                // 3. Phân tích JSON
//                return ParseVoiceAssessmentQuestionsResponse(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi tạo câu hỏi assessment, sử dụng fallback.");
//                // Trả về dữ liệu mẫu (fallback) nếu API lỗi
//                return GetFallbackVoiceQuestionsWithVietnamese(languageCode, languageName);
//            }
//        }

//        /// <summary>
//        /// ✅ Đánh giá hàng loạt: sử dụng TRANSCRIPT (không gửi audio) theo yêu cầu
//        /// </summary>
//        public async Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(
//            List<VoiceAssessmentQuestion> questions,
//            string languageCode,
//            string languageName,
//            List<string>? programLevelNames = null)
//        {
//            try
//            {
//                _logger.LogInformation("Bắt đầu đánh giá hàng loạt {Count} câu hỏi bằng transcript.", questions.Count);

//                // Xây prompt chứa đầy đủ transcript của các câu hợp lệ
//                var prompt = BuildBatchVoiceEvaluationPrompt(questions, languageCode, languageName, programLevelNames);
//                var parts = new List<object> { new { text = prompt } };

//                // Không gửi audio inlineData nữa – chỉ text transcripts
//                var response = await CallGeminiApiWithPartsAsync(parts);

//                return ParseBatchEvaluationResponse(response, languageName);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi đánh giá hàng loạt");
//                return CreateFallbackBatchEvaluation(questions, languageName);
//            }
//        }

//        public async Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(
//            VoiceAssessmentQuestion question,
//            IFormFile audioFile,
//            string languageCode)
//        {
//            try
//            {
//                // Giữ nguyên: API này vẫn dùng audio nếu được gọi đơn lẻ
//                var audioBase64 = await ConvertAudioToBase64Async(audioFile);
//                var prompt = BuildVoiceEvaluationPromptWithAudio(question, languageCode);
//                var mime = !string.IsNullOrWhiteSpace(audioFile.ContentType)
//                    ? audioFile.ContentType
//                    : GuessMimeFromFilePath(audioFile.FileName) ?? "audio/mpeg";

//                var parts = new List<object>
//                {
//                    new { text = prompt },
//                    new
//                    {
//                        inlineData = new
//                        {
//                            mimeType = mime,
//                            data = audioBase64
//                        }
//                    }
//                };

//                var response = await CallGeminiApiWithPartsAsync(parts);
//                return ParseVoiceEvaluationResponse(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi đánh giá trực tiếp");
//                return CreateFallbackVoiceEvaluation();
//            }
//        }


//        // ============ CÁC HÀM KHÁC ============

//        public async Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context)
//        {
//            try
//            {
//                _logger.LogInformation("Generating conversation content for {Language} - {Topic}",
//                    context.Language, context.Topic);

//                var prompt = BuildConversationPrompt(context);
//                var response = await CallGeminiApiAsync(prompt);

//                return ParseConversationResponse(response, context);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating conversation content");
//                return CreateFallbackConversationContent(context);
//            }
//        }

//        public async Task<string> GenerateResponseAsync(
//            string systemPrompt,
//            string userMessage,
//            List<string> conversationHistory)
//        {
//            try
//            {
//                var prompt = BuildResponsePrompt(systemPrompt, userMessage, conversationHistory);
//                return await CallGeminiApiAsync(prompt);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating AI response");
//                return "I understand. Could you tell me more about that?";
//            }
//        }

//        public async Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt)
//        {
//            try
//            {
//                var response = await CallGeminiApiAsync(evaluationPrompt);
//                return ParseEvaluationResponse(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error evaluating conversation");
//                return CreateFallbackEvaluationResult();
//            }
//        }

//        public async Task<AiCourseRecommendationDto> GenerateCourseRecommendationsAsync(
//            UserSurveyResponseDto survey,
//            List<CourseInfoDto> availableCourses)
//        {
//            try
//            {
//                var prompt = BuildCourseRecommendationPrompt(survey, availableCourses);
//                var response = await CallGeminiApiAsync(prompt);
//                return ParseCourseRecommendationResponse(response, availableCourses);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating course recommendations");
//                return new AiCourseRecommendationDto
//                {
//                    RecommendedCourses = new List<CourseRecommendationDto>(),
//                    ReasoningExplanation = "Cannot generate recommendations. Please try again.",
//                    LearningPath = "Please select courses manually.",
//                    StudyTips = new List<string> { "Study daily", "Practice regularly", "Find resources" },
//                    GeneratedAt = DateTime.UtcNow
//                };
//            }
//        }

//        public async Task<string> GenerateStudyPlanAsync(UserSurveyResponseDto survey)
//        {
//            try
//            {
//                var prompt = BuildStudyPlanPrompt(survey);
//                return await CallGeminiApiAsync(prompt);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating study plan");
//                return "Unable to generate study plan. Please try again later.";
//            }
//        }

//        public async Task<List<string>> GenerateStudyTipsAsync(UserSurveyResponseDto survey)
//        {
//            try
//            {
//                var prompt = BuildStudyTipsPrompt(survey);
//                var response = await CallGeminiApiAsync(prompt);
//                return ParseStudyTipsResponse(response);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating study tips");
//                return new List<string>
//                {
//                    "Study daily for15-30 minutes",
//                    "Practice all four skills: listening, speaking, reading, writing",
//                    "Watch movies and listen to music in the target language",
//                    "Find a language exchange partner"
//                };
//            }
//        }

//        public async Task<TeacherQualificationAnalysisDto> AnalyzeTeacherQualificationsAsync(
//            TeacherApplicationDto application,
//            List<TeacherCredentialDto> credentials)
//        {
//            try
//            {
//                var prompt = BuildTeacherQualificationPrompt(application, credentials);
//                var response = await CallGeminiApiAsync(prompt);
//                return ParseTeacherQualificationResponse(response, application);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error analyzing teacher qualifications");
//                return CreateFallbackQualificationAnalysis(application, credentials);
//            }
//        }

//        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
//        {
//            try
//            {
//                var prompt = $@"Translate the following text from {sourceLanguage} to {targetLanguage}.
//Only provide the translation, nothing else.
//Text: {text}";

//                var request = new
//                {
//                    contents = new[]
// {
// new { parts = new[] { new { text = prompt } } }
// },
//                    generationConfig = new
//                    {
//                        responseMimeType = "text/plain",
//                        temperature = 0.3,
//                        maxOutputTokens = 4096
//                    }
//                };

//                var response = await PostToGeminiAsync(request);

//                if (!response.IsSuccessStatusCode)
//                    return text;

//                var jsonString = await response.Content.ReadAsStringAsync();

//                using (JsonDocument doc = JsonDocument.Parse(jsonString))
//                {
//                    var root = doc.RootElement;
//                    if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
//                    {
//                        var cand0 = candidates[0];
//                        if (cand0.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
//                        {
//                            foreach (var part in parts.EnumerateArray())
//                            {
//                                if (part.TryGetProperty("text", out var t))
//                                {
//                                    var val = t.GetString();
//                                    if (!string.IsNullOrWhiteSpace(val)) return val.Trim();
//                                }
//                            }
//                        }
//                    }
//                }
//                return text;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error translating text");
//                return text;
//            }
//        }

//        // NEW: Generate synonym suggestions
//        public async Task<SynonymSuggestionDto> GenerateSynonymSuggestionsAsync(string userMessage, string targetLanguage, string currentLevel)
//        {
//            try
//            {
//                // Determine next level only
//                var nextLevel = GetNextLevel(currentLevel);
                
//                var prompt = $@"User said: ""{userMessage}"" in {targetLanguage} (current level: {currentLevel})

//Provide 2-3 BETTER alternatives at the NEXT proficiency level only ({nextLevel}).
//Do NOT suggest multiple levels - focus on natural progression.

//Example:
//If user (A2) said: ""I want to buy this""
//Suggest (B1): ""I would like to purchase this"" or ""I'd like to buy this item""
//NOT B2/C1/C2 - only the NEXT level!

//Return JSON:
//{{
//  ""originalMessage"": ""{userMessage}"",
//  ""currentLevel"": ""{currentLevel}"",
//  ""alternatives"": [
//    {{
//      ""level"": ""{nextLevel}"",
//      ""alternativeText"": ""better expression at {nextLevel}"",
//      ""difference"": ""why this is more advanced"",
//      ""exampleUsage"": ""example in context""
//    }}
//  ],
//  ""explanation"": ""brief summary""
//}}

//Return 2-3 alternatives only, all at level {nextLevel}.";

//                var response = await CallGeminiApiAsync(prompt);
//                var cleanedResponse = CleanJsonResponse(response);
                
//                var result = JsonSerializer.Deserialize<SynonymSuggestionDto>(cleanedResponse, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true
//                });

//                return result ?? new SynonymSuggestionDto
//                {
//                    OriginalMessage = userMessage,
//                    CurrentLevel = currentLevel,
//                    Alternatives = new()
//                };
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error generating synonym suggestions");
//                return new SynonymSuggestionDto
//                {
//                    OriginalMessage = userMessage,
//                    CurrentLevel = currentLevel,
//                    Alternatives = new(),
//                    Explanation = "Unable to generate suggestions at this time."
//                };
//            }
//        }

//        private string GetNextLevel(string currentLevel)
//        {
//            var levelMap = new Dictionary<string, string>
//            {
//                { "A1", "A2" }, { "A2", "B1" }, { "B1", "B2" }, { "B2", "C1" }, { "C1", "C2" }, { "C2", "C2" },
//                { "N5", "N4" }, { "N4", "N3" }, { "N3", "N2" }, { "N2", "N1" }, { "N1", "N1" },
//                { "HSK1", "HSK2" }, { "HSK2", "HSK3" }, { "HSK3", "HSK4" }, { "HSK4", "HSK5" }, { "HSK5", "HSK5" }
//            };
            
//            return levelMap.TryGetValue(currentLevel, out var next) ? next : currentLevel;
//        }

//        // ===== Prompts, Parsers, Helpers =====

//        private string BuildVoiceAssessmentQuestionsPrompt(string languageCode, string languageName, string? programName)
//        {
//            string context = string.IsNullOrEmpty(programName)
//                ? $"một bài kiểm tra trình độ {languageName} chung."
//                : $"một bài kiểm tra đầu vào cho chương trình '{programName}' (ngôn ngữ {languageName}).";

//            return $@"Bạn là AI tạo đề thi.
//Tạo4 câu hỏi đánh giá giọng nói cho {context}.
//Các câu hỏi PHẢI tuân thủ100% định dạng JSON bên dưới.
//Các câu hỏi phải có độ khó tăng dần (A1, A2, B1, B2).
//Tất cả các trường (question, promptText, vietnameseTranslation) là BẮT BUỘC.

//ĐỊNH DẠNG JSON (Chỉ trả về JSON, không markdown):
//[
// {{
// ""questionNumber"":1,
// ""question"": ""(Tiêu đề câu hỏi, ví dụ: Đọc các từ sau)"",
// ""promptText"": ""(Văn bản user cần đọc, ví dụ: Hello - World)"",
// ""vietnameseTranslation"": ""(Bản dịch tiếng Việt của promptText)"",
// ""wordGuides"": [ {{ ""word"": ""Hello"", ""pronunciation"": ""/həˈloʊ/"", ""vietnameseMeaning"": ""Xin chào"" }} ],
// ""questionType"": ""pronunciation"",
// ""difficulty"": ""A1"",
// ""maxRecordingSeconds"":30
// }},
// {{
// ""questionNumber"":2,
// ""question"": ""(Tiêu đề câu hỏi, ví dụ: Giới thiệu bản thân)"",
// ""promptText"": ""(Yêu cầu chi tiết, ví dụ: Hãy nói tên và sở thích của bạn.)"",
// ""vietnameseTranslation"": ""(Bản dịch tiếng Việt của promptText)"",
// ""wordGuides"": [],
// ""questionType"": ""speaking"",
// ""difficulty"": ""A2"",
// ""maxRecordingSeconds"":60
// }},
// {{
// ""questionNumber"":3,
// ""question"": ""(Tiêu đề câu hỏi, ví dụ: Mô tả một kỷ niệm)"",
// ""promptText"": ""(Yêu cầu chi tiết, ví dụ: Hãy kể về một kỳ nghỉ đáng nhớ.)"",
// ""vietnameseTranslation"": ""(Bản dịch tiếng Việt của promptText)"",
// ""wordGuides"": [],
// ""questionType"": ""speaking"",
// ""difficulty"": ""B1"",
// ""maxRecordingSeconds"":90
// }},
// {{
// ""questionNumber"":4,
// ""question"": ""(Tiêu đề câu hỏi, ví dụ: Thảo luận về công nghệ)"",
// ""promptText"": ""(Yêu cầu chi tiết, ví dụ: Công nghệ đã thay đổi cuộc sống của bạn như thế nào?)"",
// ""vietnameseTranslation"": ""(Bản dịch tiếng Việt của promptText)"",
// ""wordGuides"": [],
// ""questionType"": ""speaking"",
// ""difficulty"": ""B2"",
// ""maxRecordingSeconds"":120
// }}
//]
//";
//        }

//        private List<VoiceAssessmentQuestion> ParseVoiceAssessmentQuestionsResponse(string response)
//        {
//            try
//            {
//                var cleanedResponse = CleanJsonResponse(response);
//                var options = new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true,
//                    AllowTrailingCommas = true
//                };
//                var questions = JsonSerializer.Deserialize<List<VoiceAssessmentQuestion>>(cleanedResponse, options);

//                if (questions != null && questions.Any())
//                {
//                    return questions;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi phân tích JSON câu hỏi assessment.");
//            }
//            return GetFallbackVoiceQuestionsWithVietnamese("EN", "English");
//        }

//        private string BuildBatchVoiceEvaluationPrompt(
//        List<VoiceAssessmentQuestion> questions,
//        string languageCode,
//        string languageName,
//        List<string>? programLevelNames = null)
//        {
//            var questionDetails = string.Join("\n\n", questions.Select(q => $@"Q{q.QuestionNumber}
//- Difficulty: {q.Difficulty}
//- Prompt: {q.PromptText}
//- Required words: {string.Join(", ", (q.WordGuides?.Select(w => w.Word) ?? Enumerable.Empty<string>()))}
//- Skipped: {q.IsSkipped}
//- Transcript: {(string.IsNullOrWhiteSpace(q.Transcript) ? "" : q.Transcript)}"));

//            string levelScalePrompt = "Sử dụng thang đo CEFR (A1, A2, B1, B2, C1, C2) hoặc HSK/JLPT.";
//            if (programLevelNames != null && programLevelNames.Any())
//            {
//                levelScalePrompt = $"QUAN TRỌNG: 'overallLevel' PHẢI là một trong các giá trị sau: [{string.Join(", ", programLevelNames)}]. Chọn giá trị phù hợp nhất.";
//            }

//            return $@"Bạn là giám khảo chấm thi nói {languageName}.
//Đánh giá {questions.Count} câu trả lời dựa trên TRANSCRIPT dưới đây (không có audio).

//{questionDetails}

//YÊU CẦU:
//1. Đánh giá từng câu (kể cả câu bị bỏ qua). Nếu transcript trống coi như không đủ dữ liệu.
//2. Cung cấp điểm tổng quan (overallScore0-100).
//3. Cung cấp trình độ tổng quan (overallLevel). {levelScalePrompt}

//TRẢ VỀ JSON (không markdown):
//{{
// ""overallLevel"": ""(Trình độ, ví dụ: B1)"",
// ""overallScore"": (Số0-100),
// ""questionResults"": [
// {{
// ""questionNumber"":1,
// ""spokenWords"": [""(từ user nói theo transcript)""],
// ""missingWords"": [""(từ bị thiếu so với prompt)""],
// ""accuracyScore"": (0-100),
// ""pronunciationScore"": (0-100),
// ""fluencyScore"": (0-100),
// ""grammarScore"": (0-100),
// ""feedback"": ""(Nhận xét câu1)""
// }}
// // (Lặp lại cho tất cả các câu hỏi)
// ],
// ""strengths"": [""(Điểm mạnh1)"", ""(Điểm mạnh2)""],
// ""weaknesses"": [""(Điểm yếu1)"", ""(Điểm yếu2)""],
// ""recommendedCourses"": [
// {{
// ""focus"": ""(Chủ đề khóa học, ví dụ: Phát âm cơ bản)"",
// ""level"": ""(Trình độ gợi ý)"",
// ""reason"": ""(Lý do gợi ý)""
// }}
// ]
//}}
//";
//        }

//        private string BuildConversationPrompt(ConversationContextDto context)
//        {
//            var exampleGoodScenario = "It's 2 PM on a rainy Wednesday. You're Sarah, 28, sitting nervously in the HR office at GlobalTech. The interviewer, Mr. Chen, just asked about your biggest failure. Your hands are cold. This job could change your career.";
//            var exampleBadScenario = "You are at a job interview discussing your experience.";
//            var exampleRole = $"Mr. Chen, HR Manager (严肃但公正 | Nghiêm túc nhưng công bằng)";
//            var exampleGoodFirstMsg = "*adjusts glasses and glances at your resume* I see you worked at TechCorp for two years, but there's a gap here. What happened between March and August 2023?";
//            var exampleBadFirstMsg = "Hello, let's discuss your work experience.";
            
//            var restaurantScenario = "It's 7:30 PM Friday at Bella Italia, a cozy restaurant downtown. You're celebrating your friend's promotion but just received a cold pasta dish. The waiter approaches with a smile, but you're disappointed and hungry.";
//            var restaurantFirstMsg = "*notices your untouched plate* Is everything alright with your meal? You haven't started eating yet...";
            
//            var interviewScenario = "Tuesday, 10 AM sharp. You're in Conference Room B at Microsoft, interviewing for Senior Developer. The hiring manager, Lisa Park, seems impressed but just noticed a 6-month gap in your LinkedIn profile. The air conditioning is too cold.";
//            var interviewFirstMsg = "*leans forward slightly* Your technical skills are impressive, but I'm curious - what were you doing between January and June last year?";
            
//            var luggageScenario = "Sunday evening, 9 PM at Tokyo Narita Airport, Baggage Claim Area 3. Your black suitcase with your presentation materials for tomorrow's meeting never arrived. Other passengers are leaving. You're exhausted from the 14-hour flight.";
//            var luggageFirstMsg = "*looks up from computer* Good evening. How can I help you today?";

//            return $@"# Generate IMMERSIVE Roleplay Conversation Scenario

//## Context
//- Language: {context.Language} ({context.LanguageCode})
//- Topic: {context.Topic}
//- Level: {context.DifficultyLevel}

//## CRITICAL REQUIREMENTS FOR IMMERSIVE ROLEPLAY:

//### 1. SCENARIO DESCRIPTION (20-40 words in {context.Language})
//Create a VIVID, SPECIFIC situation with:
//- Exact time and place: e.g. Monday morning 9 AM at Starbucks on 5th Avenue NOT at a coffee shop
//- Character details: Name, age, appearance, mood
//- Immediate situation: What just happened that triggers this conversation
//- Emotional context: Why this conversation matters NOW
//- Sensory details: weather, sounds, atmosphere

//Example GOOD scenario: {exampleGoodScenario}
//Example BAD scenario (too generic): {exampleBadScenario}

//### 2. AI ROLE (in {context.Language})
//CRITICAL: AI role MUST MATCH the scenario context!
//- For job interview scenario → AI must be ""Hiring Manager"" or ""Interviewer"", NOT the candidate
//- For restaurant scenario → AI must be ""Server"" or ""Waiter"", NOT the customer
//- For lost luggage → AI must be ""Airport Agent"", NOT the passenger
//- Specific character name plus role
//- Brief personality trait in parentheses
//- Example: {exampleRole}

//### 3. FIRST MESSAGE - MUST BE IN-CHARACTER DIALOGUE
//The AI first message MUST:
//- Be spoken AS THE CHARACTER not describing the situation
//- MATCH the AI's role (interviewer asks questions, waiter offers menu, agent asks for flight number)
//- Show personality through word choice and tone
//- Reference specific details from the scenario
//- Create immediate engagement
//- Include a direct question or action that demands response

//Example GOOD first message: {exampleGoodFirstMsg}
//Example BAD first message (too generic): {exampleBadFirstMsg}

//### 4. TASKS (Exactly 3 tasks)
//Each task should be:
//- A specific action in this scenario context
//- Progressive difficulty
//- Bilingual format: {context.Language} pipe Vietnamese

//## Return Format (JSON only no markdown):
//Return a JSON object with: scenarioDescription aiRole systemPrompt firstMessage tasks

//VERIFY ROLE CONSISTENCY:
//- Interview scenario → aiRole MUST be interviewer/hiring manager
//- Restaurant → aiRole MUST be server/waiter
//- Luggage → aiRole MUST be airport agent/staff
//- NO ROLE MISMATCH!

//systemPrompt MUST include these instructions:
//You are [character name], [role] at [location]. Current situation: [brief scenario].

//ABSOLUTE RULES:
//1. Reply ONLY in {context.Language} - NEVER use other languages
//2. Stay 100% in character - you are [role], not a teacher or assistant
//3. Keep responses 1-2 sentences, natural spoken dialogue
//4. Show personality through word choice and tone
//5. Reference scenario details naturally in responses

//HANDLING OFF-TOPIC:
//If user asks about something unrelated to {context.Topic} or your role:
//- Politely acknowledge in-character
//- Briefly explain you cannot help with that
//- Redirect to the topic/situation
//Example: If user asks barista about shoelaces: ""Sorry, I don't have shoelaces here - this is a coffee shop! But I can definitely help you with a drink or snack. What can I get you?""

//STRICT FORMATTING:
//- NO emojis
//- NO markdown (**, __, etc)
//- NO role labels (""AI:"", ""Barista:"")
//- NO meta-commentary (""As a barista..."")
//- Use *action* for brief physical actions only

//Language level: {context.DifficultyLevel}
//- Use vocabulary and grammar appropriate for this level
//- Keep sentence structure simple for A1-A2, more complex for B2+

//## EXAMPLES OF GOOD SCENARIOS BY TOPIC:

//Restaurant B1 level:
//scenarioDescription: {restaurantScenario}
//aiRole: Marco, Head Waiter (attentive and professional)
//firstMessage: {restaurantFirstMsg}
//systemPrompt: You are Marco, head waiter at Bella Italia. The customer just received cold pasta and looks disappointed. Reply ONLY in English. Stay in character as a professional waiter - acknowledge issues, offer solutions, maintain hospitality. Keep responses 1-2 sentences. If customer asks about non-restaurant topics, politely redirect: ""I'm here to help with your meal, not [topic]. Now, about your pasta..."" NO emojis, NO markdown.

//Job Interview B2 level:
//scenarioDescription: {interviewScenario}
//aiRole: Lisa Park, Hiring Manager (sharp but fair)
//firstMessage: {interviewFirstMsg}
//systemPrompt: You are Lisa Park, hiring manager at Microsoft interviewing a Senior Developer candidate. You noticed a resume gap. Reply ONLY in English. Stay in character as interviewer - ask probing questions, evaluate responses, maintain professionalism. Keep responses 1-2 sentences. If candidate goes off-topic, redirect: ""That's interesting, but let's focus on your qualifications. About that gap..."" NO emojis, NO markdown.

//Lost Luggage A2 level:
//scenarioDescription: {luggageScenario}
//aiRole: James, Baggage Service Agent (calm and helpful)
//firstMessage: {luggageFirstMsg}
//systemPrompt: You are James, baggage service agent at London Heathrow Airport. Passenger's luggage is missing and they have a meeting tomorrow. Reply ONLY in English. Stay in character as airport staff - gather information, explain process, reassure customer. Keep responses 1-2 sentences, simple vocabulary. If passenger asks unrelated questions, redirect: ""I understand, but let's find your bag first. What was your flight number?"" NO emojis, NO markdown.

//Topic: {context.Topic}
//Difficulty Level: {context.DifficultyLevel}
//Additional Context: {context.ScenarioGuidelines}
//Roleplay Notes: {context.RoleplayInstructions}

//Return ONLY valid JSON no extra text or markdown.";
//        }

//        private string BuildResponsePrompt(
//        string systemPrompt,
//        string userMessage,
//        List<string> conversationHistory)
//        {
//            var historyText = string.Join("\n", conversationHistory.TakeLast(10));
//            return $@"System: {systemPrompt}

//CRITICAL: DO NOT prefix your response with ""AI:"" or any role label.
//Respond directly as the character without any prefix.

//Conversation History:
//{historyText}
//User: {userMessage}

//Your response (in-character, no prefix):";
//        }

//        private string BuildCourseRecommendationPrompt(UserSurveyResponseDto survey, List<CourseInfoDto> courses)
//        {
//            var coursesJson = JsonSerializer.Serialize(courses, new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                WriteIndented = true
//            });

//            return $@"Analyze this learner and recommend3-5 best courses:
//Current Level: {survey.CurrentLevel}
//Language: {survey.PreferredLanguageName}
//Reason: {survey.LearningReason}
//Learning Style: {survey.PreferredLearningStyle}
//Topics: {survey.InterestedTopics}
//Priority Skills: {survey.PrioritySkills}
//Available Courses:
//{coursesJson}
//Return JSON with:
//- recommendedCourses (with courseId, matchScore0-100, matchReason)
//- reasoningExplanation
//- learningPath
//- studyTips (array)
//Focus on matching level, goals, and learning style.";
//        }

//        private string BuildStudyPlanPrompt(UserSurveyResponseDto survey)
//        {
//            return $@"Create a detailed weekly study plan for:
//- Level: {survey.CurrentLevel}
//- Language: {survey.PreferredLanguageName}
//- Target Timeline: {survey.TargetTimeline}
//- Priority Skills: {survey.PrioritySkills}
//Include daily activities, goals, and assessment methods.";
//        }

//        private string BuildStudyTipsPrompt(UserSurveyResponseDto survey)
//        {
//            return $@"Provide8-10 specific study tips for:
//- Learning Style: {survey.PreferredLearningStyle}
//- Level: {survey.CurrentLevel}
//Format each tip on a new line starting with a dash.";
//        }

//        private string BuildTeacherQualificationPrompt(TeacherApplicationDto application, List<TeacherCredentialDto> credentials)
//        {
//            var credentialsJson = JsonSerializer.Serialize(credentials.Select(c => new
//            {
//                c.CredentialName,
//                c.CredentialFileUrl
//            }), new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                WriteIndented = true
//            });

//            return $@"Analyze teacher qualifications:
//Applicant: {application.UserName}
//Language: {application.LanguageName}
//Experience: {application.TeachingExperience}
//Specialization: {application.Specialization}
//Desired Levels: {application.TeachingLevel}
//Credentials:
//{credentialsJson}
//Return JSON with:
//- suggestedTeachingLevels (array)
//- confidenceScore (0-100)
//- reasoningExplanation
//- qualificationAssessments (array with credentialName, relevanceScore, assessment, supportedLevels)
//- overallRecommendation";
//        }

//        private GeneratedConversationContentDto ParseConversationResponse(
//        string response,
//        ConversationContextDto context)
//        {
//            try
//            {
//                _logger.LogInformation("Parsing conversation response with dual language");
//                var cleanedResponse = CleanJsonResponse(response);
//                var parsed = JsonSerializer.Deserialize<GeneratedConversationContentDto>(
//                cleanedResponse,
//                new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true,
//                    AllowTrailingCommas = true
//                });

//                if (parsed != null)
//                {
//                    if (parsed.Tasks == null || parsed.Tasks.Count == 0)
//                    {
//                        _logger.LogWarning("No tasks in response, creating defaults");
//                        parsed.Tasks = CreateDefaultTasks(context.Topic, context.Language);
//                    }
//                    _logger.LogInformation("Successfully parsed with {TaskCount} tasks in {Language}",
//                    parsed.Tasks.Count, context.Language);
//                    return parsed;
//                }
//            }
//            catch (JsonException ex)
//            {
//                _logger.LogError(ex, "JSON parsing error");
//            }
//            return CreateFallbackConversationContent(context);
//        }

//        private ConversationEvaluationResult ParseEvaluationResponse(string response)
//        {
//            try
//            {
//                var cleanedResponse = CleanJsonResponse(response);
//                var parsed = JsonSerializer.Deserialize<ConversationEvaluationDto>(
//                cleanedResponse,
//                new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true
//                });

//                if (parsed != null)
//                {
//                    return new ConversationEvaluationResult
//                    {
//                        OverallScore = parsed.OverallScore,
//                        FluentScore = parsed.FluentScore,
//                        GrammarScore = parsed.GrammarScore,
//                        VocabularyScore = parsed.VocabularyScore,
//                        CulturalScore = parsed.CulturalScore,
//                        AIFeedback = parsed.AIFeedback,
//                        Improvements = parsed.Improvements,
//                        Strengths = parsed.Strengths
//                    };
//                }
//                return CreateFallbackEvaluationResult();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error parsing evaluation");
//                return CreateFallbackEvaluationResult();
//            }
//        }

//        private AiCourseRecommendationDto ParseCourseRecommendationResponse(string response, List<CourseInfoDto> availableCourses)
//        {
//            try
//            {
//                var cleanedResponse = CleanJsonResponse(response);
//                var aiResponse = JsonSerializer.Deserialize<AiResponseFormat>(cleanedResponse, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true
//                });

//                if (aiResponse?.RecommendedCourses != null)
//                {
//                    var recommendations = new List<CourseRecommendationDto>();
//                    foreach (var rec in aiResponse.RecommendedCourses)
//                    {
//                        var course = availableCourses.FirstOrDefault(c =>
//                        c.CourseID.ToString().Equals(rec.CourseId, StringComparison.OrdinalIgnoreCase));

//                        if (course != null)
//                        {
//                            recommendations.Add(new CourseRecommendationDto
//                            {
//                                CourseID = course.CourseID,
//                                CourseName = course.Title,
//                                CourseDescription = course.Description,
//                                Level = course.Level,
//                                MatchScore = Math.Min(100, Math.Max(0, rec.MatchScore)),
//                                MatchReason = rec.MatchReason ?? "Suitable for your goals",
//                                EstimatedDuration = course.Duration
//                            });
//                        }
//                    }

//                    return new AiCourseRecommendationDto
//                    {
//                        RecommendedCourses = recommendations,
//                        ReasoningExplanation = aiResponse.ReasoningExplanation ?? "AI analysis completed.",
//                        LearningPath = aiResponse.LearningPath ?? "Start with basic level and progress gradually.",
//                        StudyTips = aiResponse.StudyTips ?? new List<string>(),
//                        GeneratedAt = DateTime.UtcNow
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error parsing course recommendations");
//            }
//            return CreateFallbackRecommendation(availableCourses);
//        }

//        private TeacherQualificationAnalysisDto ParseTeacherQualificationResponse(string response, TeacherApplicationDto application)
//        {
//            try
//            {
//                var cleanedResponse = CleanJsonResponse(response);
//                var aiResponse = JsonSerializer.Deserialize<TeacherQualificationAiResponse>(cleanedResponse, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true
//                });

//                if (aiResponse != null)
//                {
//                    return new TeacherQualificationAnalysisDto
//                    {
//                        ApplicationId = application.TeacherApplicationID,
//                        LanguageName = application.LanguageName,
//                        SuggestedTeachingLevels = aiResponse.SuggestedTeachingLevels ?? new List<string>(),
//                        ConfidenceScore = Math.Min(100, Math.Max(0, aiResponse.ConfidenceScore)),
//                        ReasoningExplanation = aiResponse.ReasoningExplanation ?? "",
//                        QualificationAssessments = aiResponse.QualificationAssessments?.Select(qa => new QualificationAssessment
//                        {
//                            CredentialName = qa.CredentialName ?? "",
//                            CredentialType = qa.CredentialType ?? "",
//                            RelevanceScore = Math.Min(100, Math.Max(0, qa.RelevanceScore)),
//                            Assessment = qa.Assessment ?? "",
//                            SupportedLevels = qa.SupportedLevels ?? new List<string>()
//                        }).ToList() ?? new List<QualificationAssessment>(),
//                        OverallRecommendation = aiResponse.OverallRecommendation ?? "",
//                        AnalyzedAt = DateTime.UtcNow
//                    };
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error parsing teacher qualifications");
//            }
//            return CreateFallbackQualificationAnalysis(application, new List<TeacherCredentialDto>());
//        }

//        private List<string> ParseStudyTipsResponse(string response)
//        {
//            var tips = new List<string>();
//            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

//            foreach (var line in lines)
//            {
//                var trimmed = line.Trim();
//                if (trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
//                {
//                    tips.Add(trimmed.Substring(2).Trim());
//                }
//            }
//            return tips.Take(10).ToList();
//        }

//        private VoiceEvaluationResult ParseVoiceEvaluationResponse(string response)
//        {
//            try
//            {
//                if (string.IsNullOrWhiteSpace(response))
//                    return CreateFallbackVoiceEvaluation();

//                var cleanedResponse = CleanJsonResponse(response);
//                var result = JsonSerializer.Deserialize<VoiceEvaluationResult>(cleanedResponse, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true
//                });

//                return result ?? CreateFallbackVoiceEvaluation();
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error parsing voice evaluation");
//                return CreateFallbackVoiceEvaluation();
//            }
//        }

//        private BatchVoiceEvaluationResult ParseBatchEvaluationResponse(string response, string languageName)
//        {
//            try
//            {
//                var cleanedResponse = CleanJsonResponse(response);
//                var result = JsonSerializer.Deserialize<BatchVoiceEvaluationResult>(cleanedResponse, new JsonSerializerOptions
//                {
//                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
//                    PropertyNameCaseInsensitive = true
//                });

//                if (result != null)
//                {
//                    result.EvaluatedAt = DateTime.UtcNow;
//                    return result;
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error parsing batch evaluation");
//            }
//            return CreateFallbackBatchEvaluation(new List<VoiceAssessmentQuestion>(), languageName);
//        }

//        private string BuildVoiceEvaluationPromptWithAudio(VoiceAssessmentQuestion question, string languageCode)
//        {
//            return $@"Evaluate this voice response:
//Question: {question.Question}
//Level: {question.Difficulty}
//Language: {languageCode}
//Required words: {string.Join(", ", question.WordGuides?.Select(w => w.Word) ?? new List<string>())}
//Assess:
//1. Pronunciation (30%)
//2. Fluency (25%)
//3. Grammar (25%)
//4. Vocabulary (20%)
//Return JSON with overallScore, pronunciation, fluency, grammar, vocabulary (each with score and feedback), strengths, areasForImprovement.";
//        }

//        // ===== HTTP helpers: model/baseUrl fallback =====
//        private async Task<HttpResponseMessage> PostToGeminiAsync(object requestBody)
//        {
//            var modelsToTry = BuildModelCandidates();
//            var baseUrls = BuildBaseUrlCandidates();

//            var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
//            {
//                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
//            });

//            foreach (var baseUrl in baseUrls)
//            {
//                foreach (var model in modelsToTry)
//                {
//                    var url = $"{baseUrl}/models/{model}:generateContent?key={_settings.ApiKey}";
//                    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
//                    var response = await _httpClient.PostAsync(url, content);

//                    if (response.StatusCode == HttpStatusCode.NotFound)
//                    {
//                        _logger.LogWarning("Model or endpoint not found: {Url}", url);
//                        continue;
//                    }

//                    return response; // success or other error handled by caller
//                }
//            }

//            return new HttpResponseMessage(HttpStatusCode.NotFound)
//            {
//                Content = new StringContent("No available Gemini model found for configured endpoints.")
//            };
//        }

//        private List<string> BuildModelCandidates()
//        {
//            var configured = string.IsNullOrWhiteSpace(_settings.Model) ? "gemini-1.5-flash-latest" : _settings.Model.Trim();
//            var list = new List<string> { configured };
//            if (!configured.EndsWith("-latest", StringComparison.OrdinalIgnoreCase))
//                list.Add(configured + "-latest");
//            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
//        }

//        private List<string> BuildBaseUrlCandidates()
//        {
//            var configured = string.IsNullOrWhiteSpace(_settings.BaseUrl)
//            ? "https://generativelanguage.googleapis.com/v1"
//            : _settings.BaseUrl.Trim().TrimEnd('/');

//            var list = new List<string> { configured };
//            if (configured.Contains("/v1beta"))
//                list.Add(configured.Replace("/v1beta", "/v1"));
//            else if (configured.EndsWith("/v1", StringComparison.OrdinalIgnoreCase))
//                list.Add(configured.Replace("/v1", "/v1beta"));

//            return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
//        }

//        private async Task<string> CallGeminiApiAsync(string prompt)
//        {
//            var requestBody = new
//            {
//                contents = new[] { new { parts = new[] { new { text = prompt } } } },
//                generationConfig = new
//                {
//                    responseMimeType = "application/json",
//                    temperature = 0.3,
//                    maxOutputTokens = 8192
//                }
//            };

//            var response = await PostToGeminiAsync(requestBody);
//            var responseContent = await response.Content.ReadAsStringAsync();

//            if (!response.IsSuccessStatusCode)
//            {
//                _logger.LogError("Gemini API error: {StatusCode}\n{Response}", response.StatusCode, responseContent);
//                throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
//            }

//            return ExtractTextFromGeminiResponse(responseContent);
//        }

//        private async Task<string> CallGeminiApiWithPartsAsync(List<object> parts)
//        {
//            try
//            {
//                var requestBody = new
//                {
//                    contents = new[] { new { parts = parts.ToArray() } },
//                    generationConfig = new
//                    {
//                        responseMimeType = "application/json",
//                        temperature = 0.2,
//                        maxOutputTokens = 8192
//                    }
//                };

//                var response = await PostToGeminiAsync(requestBody);
//                var responseContent = await response.Content.ReadAsStringAsync();

//                if (!response.IsSuccessStatusCode)
//                {
//                    _logger.LogError("Gemini API (audio) error: {StatusCode}\n{Response}", response.StatusCode, responseContent);
//                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
//                }

//                return ExtractTextFromGeminiResponse(responseContent);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Lỗi khi gọi Gemini API (với audio/parts)");
//                throw;
//            }
//        }

//        private string ExtractTextFromGeminiResponse(string responseContent)
//        {
//            try
//            {
//                using var doc = JsonDocument.Parse(responseContent);
//                if (!doc.RootElement.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
//                    return string.Empty;

//                var cand0 = candidates[0];

//                if (cand0.TryGetProperty("finishReason", out var finish) && finish.GetString() == "MAX_TOKENS")
//                {
//                    _logger.LogWarning("Gemini finishReason=MAX_TOKENS. Consider reducing prompt or increasing maxOutputTokens.");
//                    return string.Empty;
//                }

//                if (cand0.TryGetProperty("content", out var content) && content.TryGetProperty("parts", out var parts))
//                {
//                    var sb = new StringBuilder();
//                    foreach (var part in parts.EnumerateArray())
//                    {
//                        if (part.TryGetProperty("text", out var t))
//                        {
//                            var txt = t.GetString();
//                            if (!string.IsNullOrWhiteSpace(txt)) sb.Append(txt);
//                        }
//                    }
//                    return sb.ToString();
//                }

//                return string.Empty;
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error extracting text from response: {Response}", responseContent);
//                return string.Empty;
//            }
//        }

//        private async Task<string> ConvertAudioToBase64Async(IFormFile audioFile)
//        {
//            try
//            {
//                // Chấp nhận mọi audio/* để tránh fail do trình duyệt ghi âm
//                if (string.IsNullOrWhiteSpace(audioFile.ContentType) || !audioFile.ContentType.StartsWith("audio"))
//                    _logger.LogWarning("Unexpected audio content type: {ContentType}", audioFile.ContentType);

//                if (audioFile.Length > 20 * 1024 * 1024) // 20MB
//                    throw new ArgumentException("Audio file must not exceed 20MB");

//                using var memoryStream = new MemoryStream();
//                await audioFile.CopyToAsync(memoryStream);
//                return Convert.ToBase64String(memoryStream.ToArray());
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error converting audio to base64");
//                throw;
//            }
//        }

//        private string CleanJsonResponse(string response)
//        {
//            var cleaned = response.Trim();

//            if (cleaned.StartsWith("```json"))
//                cleaned = cleaned.Replace("```json", "").Replace("```", "").Trim();
//            else if (cleaned.StartsWith("```"))
//                cleaned = cleaned.Replace("```", "").Trim();

//            var start = cleaned.IndexOf('{');
//            var end = cleaned.LastIndexOf('}');

//            if (cleaned.StartsWith("["))
//            {
//                start = cleaned.IndexOf('[');
//                end = cleaned.LastIndexOf(']');
//            }

//            if (start >= 0 && end > start)
//                return cleaned.Substring(start, (end - start) + 1);

//            return cleaned;
//        }

//        private static string? GuessMimeFromFilePath(string path)
//        {
//            try
//            {
//                var ext = Path.GetExtension(path).ToLowerInvariant();
//                return ext switch
//                {
//                    ".mp3" => "audio/mpeg",
//                    ".wav" => "audio/wav",
//                    ".m4a" => "audio/m4a",
//                    ".aac" => "audio/aac",
//                    ".webm" => "audio/webm",
//                    ".ogg" => "audio/ogg",
//                    _ => null
//                };
//            }
//            catch
//            {
//                return null;
//            }
//        }

//        private GeneratedConversationContentDto CreateFallbackConversationContent(ConversationContextDto context)
//        {
//            var (scenario, role, firstMsg) = GetImmersiveFallback(context.Topic, context.Language, context.DifficultyLevel);
            
//            return new GeneratedConversationContentDto
//            {
//                ScenarioDescription = scenario,
//                AIRole = role,
//                SystemPrompt = $@"You are {role}. Current situation: {scenario}

//ABSOLUTE RULES:
//1. Reply ONLY in {context.Language} - NEVER use other languages
//2. Stay 100% in character as {role} - not a teacher or assistant
//3. Keep responses 1-2 sentences, natural spoken dialogue
//4. Show personality through word choice and tone

//HANDLING OFF-TOPIC:
//If user asks about something unrelated to {context.Topic}:
//- Acknowledge politely in-character
//- Briefly say you cannot help with that
//- Redirect back to the topic
//Example: ""I understand, but I can't help with that here. Let's focus on [topic]. [relevant question]""

//FORMATTING:
//- NO emojis
//- NO markdown (**, __, etc)  
//- NO role labels
//- Use *action* for brief actions only

//Language level: {context.DifficultyLevel} - use appropriate vocabulary and grammar.",
//                FirstMessage = firstMsg,
//                Tasks = GetContextualTasks(context.Topic, context.Language)
//            };
//        }

//        private (string scenario, string role, string firstMessage) GetImmersiveFallback(string topic, string language, string level)
//        {
//            var isBasic = level.Contains("A1") || level.Contains("N5") || level.Contains("HSK1");
//            var topicLower = topic.ToLower();

//            if (topicLower.Contains("restaurant") || topicLower.Contains("ẩm thực"))
//            {
//                if (language.Contains("English"))
//                    return (
//                        "It's 7 PM Friday at Bella Italia, a cozy downtown restaurant. You're Sarah, celebrating your friend's promotion. The waiter just brought your pasta, but it's cold. You're hungry and disappointed.",
//                        "Marco, Head Waiter (attentive and professional)",
//                        "*approaches your table with a concerned look* I notice you haven't touched your pasta yet. Is something wrong with the dish?"
//                    );
//                if (language.Contains("Japanese") || language.Contains("日本"))
//                    return (
//                        "金曜日の夜7時、銀座の「イタリア亭」というレストランです。あなたは友達の昇進を祝っています。パスタが冷たくて、がっかりしています。",
//                        "マルコ、ウェイター長 (丁寧でプロ)",
//                        "*心配そうにテーブルに近づく* パスタにまだ手をつけていませんね。何か問題がございますか？"
//                    );
//                if (language.Contains("Chinese") || language.Contains("中文"))
//                    return (
//                        "周五晚上7点，你在市中心的意大利餐厅庆祝朋友升职。服务员刚端来你的意大利面，但是凉的。你又饿又失望。",
//                        "马可，服务生领班 (细心专业)",
//                        "*担心地走近您的桌子* 我注意到您还没动您的意大利面。菜有什么问题吗？"
//                    );
//            }

//            if (topicLower.Contains("interview") || topicLower.Contains("phỏng vấn") || topicLower.Contains("面接"))
//            {
//                if (language.Contains("English"))
//                    return (
//                        "Tuesday 10 AM, Conference Room B at TechCorp. You're interviewing for Senior Developer. Lisa Park, the hiring manager, just noticed a 6-month gap in your resume. The room is cold.",
//                        "Lisa Park, Hiring Manager (sharp but fair)",
//                        "*leans forward and points at your resume* Your skills look solid, but what were you doing between January and June last year?"
//                    );
//                if (language.Contains("Japanese") || language.Contains("日本"))
//                    return (
//                        "火曜日午前10時、テックコープの会議室Bです。シニア開発者のポジションの面接中。人事マネージャーの山田さんが履歴書の6ヶ月の空白に気づきました。",
//                        "山田, 人事マネージャー (鋭いが公平)",
//                        "*履歴書を指差しながら* スキルは素晴らしいですが、去年の1月から6月は何をされていましたか？"
//                    );
//                if (language.Contains("Chinese") || language.Contains("中文"))
//                    return (
//                        "周二上午10点，科技公司B会议室。您正在面试高级开发工程师职位。人事经理李女士注意到您简历上有6个月的空白期。",
//                        "李经理，人事经理 (敏锐公正)",
//                        "*指着简历* 您的技能很不错，但去年1月到6月这段时间您在做什么？"
//                    );
//            }

//            if (topicLower.Contains("luggage") || topicLower.Contains("hành lý") || topicLower.Contains("荷物"))
//            {
//                if (language.Contains("English"))
//                    return (
//                        "Sunday 9 PM, Baggage Claim Area 3 at London Heathrow. Your black suitcase with important documents didn't arrive. You have a meeting tomorrow morning. You're exhausted after a 12-hour flight.",
//                        "James, Baggage Service Agent (calm and helpful)",
//                        isBasic 
//                            ? "*looks up from computer* Good evening. What's your flight number?"
//                            : "*looks up from computer* I can see you're concerned. Let me help - which flight were you on?"
//                    );
//                if (language.Contains("Japanese") || language.Contains("日本"))
//                    return (
//                        "日曜日午後9時、成田空港の手荷物受取所3番です。大事な書類が入った黒いスーツケースが届いていません。明日の朝、会議があります。",
//                        "田中、荷物サービス係 (落ち着いて親切)",
//                        isBasic
//                            ? "*パソコンから顔を上げて* こんばんは。フライト番号を教えていただけますか？"
//                            : "*パソコンから顔を上げて* お困りのようですね。お手伝いします。どのフライトでしたか？"
//                    );
//                if (language.Contains("Chinese") || language.Contains("中文"))
//                    return (
//                        "周日晚上9点，北京机场3号行李提取处。您的黑色行李箱没到，里面有重要文件。明天早上有会议。经过12小时飞行，您很疲惫。",
//                        "王先生，行李服务员 (冷静热心)",
//                        isBasic
//                            ? "*从电脑前抬起头* 晚上好。您的航班号是多少？"
//                            : "*从电脑前抬起头* 看起来您很着急。我来帮您，您是哪个航班？"
//                    );
//            }

//            // Generic fallback
//            return (
//                $"Practice {topic} conversation in a realistic setting. You're in a professional environment discussing {topic}.",
//                "Conversation Partner (helpful and engaged)",
//                isBasic 
//                    ? $"Hello! Ready to practice about {topic}?"
//                    : $"Hi there! I'm looking forward to discussing {topic} with you. What would you like to start with?"
//            );
//        }

//        private List<ConversationTaskDto> GetContextualTasks(string topic, string language)
//        {
//            var topicLower = topic.ToLower();
            
//            if (topicLower.Contains("restaurant") || topicLower.Contains("ẩm thực"))
//            {
//                return language.Contains("English") 
//                    ? new List<ConversationTaskDto> {
//                        new() { TaskDescription = "Politely explain the problem with your food", TaskSequence = 1 },
//                        new() { TaskDescription = "Ask what the restaurant can do to resolve it", TaskSequence = 2 },
//                        new() { TaskDescription = "Reach a satisfactory solution", TaskSequence = 3 }
//                    }
//                    : language.Contains("Japanese")
//                    ? new List<ConversationTaskDto> {
//                        new() { TaskDescription = "料理の問題を丁寧に説明する", TaskSequence = 1 },
//                        new() { TaskDescription = "解決方法を尋ねる", TaskSequence = 2 },
//                        new() { TaskDescription = "満足できる解決策に達する", TaskSequence = 3 }
//                    }
//                    : new List<ConversationTaskDto> {
//                        new() { TaskDescription = "礼貌地说明菜品问题", TaskSequence = 1 },
//                        new() { TaskDescription = "询问餐厅的解决方案", TaskSequence = 2 },
//                        new() { TaskDescription = "达成满意的解决办法", TaskSequence = 3 }
//                    };
//            }

//            if (topicLower.Contains("interview") || topicLower.Contains("phỏng vấn"))
//            {
//                return language.Contains("English")
//                    ? new List<ConversationTaskDto> {
//                        new() { TaskDescription = "Explain your career gap honestly and positively", TaskSequence = 1 },
//                        new() { TaskDescription = "Highlight skills you developed during that time", TaskSequence = 2 },
//                        new() { TaskDescription = "Connect it to why you're perfect for this role", TaskSequence = 3 }
//                    }
//                    : new List<ConversationTaskDto> {
//                        new() { TaskDescription = "诚实积极地解释职业空白期", TaskSequence = 1 },
//                        new() { TaskDescription = "强调那段时间学到的技能", TaskSequence = 2 },
//                        new() { TaskDescription = "说明为何您适合这个职位", TaskSequence = 3 }
//                    };
//            }

//            if (topicLower.Contains("luggage") || topicLower.Contains("hành lý"))
//            {
//                return language.Contains("English")
//                    ? new List<ConversationTaskDto> {
//                        new() { TaskDescription = "Describe your missing luggage clearly", TaskSequence = 1 },
//                        new() { TaskDescription = "Ask about the search process and timeline", TaskSequence = 2 },
//                        new() { TaskDescription = "Arrange delivery to your hotel", TaskSequence = 3 }
//                    }
//                    : new List<ConversationTaskDto> {
//                        new() { TaskDescription = "清楚描述丢失的行李", TaskSequence = 1 },
//                        new() { TaskDescription = "询问查找流程和时间", TaskSequence = 2 },
//                        new() { TaskDescription = "安排送到酒店", TaskSequence = 3 }
//                    };
//            }

//            // Generic tasks
//            return new List<ConversationTaskDto> {
//                new() { TaskDescription = "Introduce yourself and explain the situation", TaskSequence = 1 },
//                new() { TaskDescription = "Ask relevant questions to gather information", TaskSequence = 2 },
//                new() { TaskDescription = "Work towards a clear resolution", TaskSequence = 3 }
//            };
//        }
//        private ConversationEvaluationResult CreateFallbackEvaluationResult()
//        {
//            return new ConversationEvaluationResult
//            {
//                OverallScore = 75,
//                FluentScore = 70,
//                GrammarScore = 80,
//                VocabularyScore = 75,
//                CulturalScore = 70,
//                AIFeedback = "Good effort!",
//                Improvements = "Keep practicing",
//                Strengths = "Good engagement"
//            };
//        }
//        private ConversationEvaluationDto CreateFallbackEvaluation()
//        {
//            return new ConversationEvaluationDto
//            {
//                OverallScore = 75,
//                FluentScore = 70,
//                GrammarScore = 80,
//                VocabularyScore = 75,
//                CulturalScore = 70,
//                AIFeedback = "Good effort!",
//                Improvements = "Keep practicing",
//                Strengths = "Good engagement"
//            };
//        }
//        private AiCourseRecommendationDto CreateFallbackRecommendation(List<CourseInfoDto> courses)
//        {
//            return new AiCourseRecommendationDto
//            {
//                RecommendedCourses = courses.Take(3).Select(c => new CourseRecommendationDto
//                {
//                    CourseID = c.CourseID,
//                    CourseName = c.Title,
//                    Level = c.Level,
//                    MatchScore = 75
//                }).ToList(),
//                ReasoningExplanation = "Popular courses for your level.",
//                LearningPath = "Start with these courses.",
//                StudyTips = new List<string> { "Study daily", "Practice regularly" },
//                GeneratedAt = DateTime.UtcNow
//            };
//        }
//        private TeacherQualificationAnalysisDto CreateFallbackQualificationAnalysis(
//        TeacherApplicationDto application,
//        List<TeacherCredentialDto> credentials)
//        {
//            return new TeacherQualificationAnalysisDto
//            {
//                ApplicationId = application.TeacherApplicationID,
//                LanguageName = application.LanguageName,
//                SuggestedTeachingLevels = new List<string> { "Beginner" },
//                ConfidenceScore = 50,
//                ReasoningExplanation = "Manual review needed.",
//                QualificationAssessments = new List<QualificationAssessment>(),
//                OverallRecommendation = "Review manually.",
//                AnalyzedAt = DateTime.UtcNow
//            };
//        }
//        private VoiceEvaluationResult CreateFallbackVoiceEvaluation()
//        {
//            return new VoiceEvaluationResult
//            {
//                OverallScore = 70,
//                Pronunciation = new PronunciationScore { Score = 70, Feedback = "Manual review needed" },
//                Fluency = new FluencyScore { Score = 70, Feedback = "Continue practicing" },
//                Grammar = new GrammarScore { Score = 70, Feedback = "Good effort" },
//                Vocabulary = new VocabularyScore { Score = 70, Feedback = "Keep learning" },
//                DetailedFeedback = "Unable to process audio. Please try again.",
//                Strengths = new List<string> { "Participated" },
//                AreasForImprovement = new List<string> { "Retry with better audio" }
//            };
//        }
//        private BatchVoiceEvaluationResult CreateFallbackBatchEvaluation(
//        List<VoiceAssessmentQuestion> questions,
//        string languageName)
//        {
//            string fallbackLevel = "Beginner";
//            if (languageName.Contains("English")) fallbackLevel = "A1";
//            if (languageName.Contains("Japanese")) fallbackLevel = "N5";
//            if (languageName.Contains("Chinese")) fallbackLevel = "HSK1";

//            return new BatchVoiceEvaluationResult
//            {
//                OverallLevel = fallbackLevel,
//                OverallScore = 0,
//                QuestionResults = questions.Select(q => new QuestionEvaluationResult
//                {
//                    QuestionNumber = q.QuestionNumber,
//                    SpokenWords = new List<string>(),
//                    MissingWords = q.WordGuides?.Select(w => w.Word).ToList() ?? new List<string>(),
//                    AccuracyScore = 0,
//                    PronunciationScore = 0,
//                    FluencyScore = 0,
//                    GrammarScore = 0,
//                    Feedback = "Không thể đánh giá do lỗi hệ thống hoặc không có audio."
//                }).ToList(),
//                Strengths = new List<string>(),
//                Weaknesses = new List<string> { "Đã xảy ra lỗi trong quá trình đánh giá AI." },
//                RecommendedCourses = new List<CourseRecommendation>(),
//                EvaluatedAt = DateTime.UtcNow
//            };
//        }
//        private List<ConversationTaskDto> CreateDefaultTasks(string topic, string language)
//        {
//            return topic.ToLower() switch
//            {
//                var t when t.Contains("restaurant") => new List<ConversationTaskDto> {
// new() { TaskDescription = "Ask for recommendations | Hỏi gợi ý", TaskSequence =1, TaskContext = "Practice ordering" },
// new() { TaskDescription = "Order your main course | Đặt món chính", TaskSequence =2, TaskContext = "Use polite phrases" },
// new() { TaskDescription = "Ask for the bill | Yêu cầu tính tiền", TaskSequence =3, TaskContext = "Complete transaction" }
// },
//                var t when t.Contains("health") => new List<ConversationTaskDto> {
// new() { TaskDescription = "Describe your symptoms | Mô tả triệu chứng", TaskSequence =1, TaskContext = "Use medical vocabulary" },
// new() { TaskDescription = "Ask about treatment | Hỏi về điều trị", TaskSequence =2, TaskContext = "Be specific" },
// new() { TaskDescription = "Ask about follow-up | Hỏi về tái khám", TaskSequence =3, TaskContext = "Show concern" }
// },
//                _ => new List<ConversationTaskDto> {
// new() { TaskDescription = $"Start the conversation | Bắt đầu", TaskSequence =1 },
// new() { TaskDescription = $"Ask follow-up questions | Đặt câu hỏi", TaskSequence =2 },
// new() { TaskDescription = $"Share your perspective | Chia sẻ quan điểm", TaskSequence =3 }
// }
//            };
//        }
//        private List<VoiceAssessmentQuestion> GetFallbackVoiceQuestionsWithVietnamese(string languageCode, string languageName)
//        {
//            return languageCode.ToUpper() switch
//            {
//                "EN" => new List<VoiceAssessmentQuestion>
// {
// new() { QuestionNumber =1, Question = "Pronounce:", PromptText = "Hello - World", VietnameseTranslation = "Xin chào - Thế giới", Difficulty = "A1", MaxRecordingSeconds =30 },
// new() { QuestionNumber =2, Question = "Introduce yourself:", PromptText = "Tell me your name and hobbies.", VietnameseTranslation = "Nói tên và sở thích.", Difficulty = "A2", MaxRecordingSeconds =60 },
// new() { QuestionNumber =3, Question = "Describe your day:", PromptText = "What do you do from morning to evening?", VietnameseTranslation = "Bạn làm gì từ sáng đến tối?", Difficulty = "B1", MaxRecordingSeconds =90 },
// new() { QuestionNumber =4, Question = "Discuss technology:", PromptText = "How has technology changed your life?", VietnameseTranslation = "Công nghệ thay đổi bạn thế nào?", Difficulty = "B2", MaxRecordingSeconds =120 }
// },
//                "ZH" => new List<VoiceAssessmentQuestion>
// {
// new() { QuestionNumber =1, Question = "请读出:", PromptText = "你好 - 世界", VietnameseTranslation = "Xin chào - Thế giới", Difficulty = "HSK1", MaxRecordingSeconds =30 },
// new() { QuestionNumber =2, Question = "请介绍你自己:", PromptText = "说出你的名字和爱好。", VietnameseTranslation = "Nói tên và sở thích.", Difficulty = "HSK2", MaxRecordingSeconds =60 },
// new() { QuestionNumber =3, Question = "描述你的家乡:", PromptText = "你的家乡是什么样的?", VietnameseTranslation = "Quê bạn thế nào?", Difficulty = "HSK3", MaxRecordingSeconds =90 },
// new() { QuestionNumber =4, Question = "谈论文化:", PromptText = "你认为中国文化的特色是什么?", VietnameseTranslation = "Đặc sắc văn hóa TQ là gì?", Difficulty = "HSK4", MaxRecordingSeconds =120 }
// },
//                "JA" => new List<VoiceAssessmentQuestion>
// {
// new() { QuestionNumber =1, Question = "読んでください:", PromptText = "こんにちは - 世界", VietnameseTranslation = "Xin chào - Thế giới", Difficulty = "N5", MaxRecordingSeconds =30 },
// new() { QuestionNumber =2, Question = "自己紹介:", PromptText = "名前と趣味を言ってください。", VietnameseTranslation = "Nói tên và sở thích.", Difficulty = "N4", MaxRecordingSeconds =60 },
// new() { QuestionNumber =3, Question = "好きな季節:", PromptText = "どの季節が好きですか?", VietnameseTranslation = "Bạn thích mùa nào?", Difficulty = "N3", MaxRecordingSeconds =90 },
// new() { QuestionNumber =4, Question = "日本文化:", PromptText = "日本文化で興味深いことは何ですか?", VietnameseTranslation = "Điều gì thú vị về VH Nhật?", Difficulty = "N2", MaxRecordingSeconds =120 }
// },
//                _ => new List<VoiceAssessmentQuestion>()
//            };
//        }
//        private string GetDefaultRole(string topicName)
//        {
//            return topicName.ToLower() switch
//            {
//                var t when t.Contains("restaurant") => "Restaurant Staff",
//                var t when t.Contains("travel") => "Travel Guide",
//                var t when t.Contains("shopping") => "Shop Assistant",
//                _ => "Conversation Partner"
//            };
//        }
//        private string GetDefaultFirstMessage(string language, string topic)
//        {
//            if (language.Contains("English"))
//                return $"Hello! Ready to practice about {topic}?";
//            if (language.Contains("Japanese"))
//                return $"こんにちは！{topic}について話しましょう！";
//            if (language.Contains("Chinese"))
//                return $"你好！我们一起聊聊{topic}吧！";
//            return "Hello! Let's start!";
//        }

//        private class AiResponseFormat
//        {
//            public List<CourseMatch>? RecommendedCourses { get; set; }
//            public string? ReasoningExplanation { get; set; }
//            public string? LearningPath { get; set; }
//            public List<string>? StudyTips { get; set; }
//        }
//        private class CourseMatch
//        {
//            public string CourseId { get; set; } = "";
//            public decimal MatchScore { get; set; }
//            public string MatchReason { get; set; } = "";
//        }
//        private class TeacherQualificationAiResponse
//        {
//            public List<string>? SuggestedTeachingLevels { get; set; }
//            public int ConfidenceScore { get; set; }
//            public string? ReasoningExplanation { get; set; }
//            public List<QualificationAssessmentAi>? QualificationAssessments { get; set; }
//            public string? OverallRecommendation { get; set; }
//        }
//        private class QualificationAssessmentAi
//        {
//            public string? CredentialName { get; set; }
//            public string? CredentialType { get; set; }
//            public int RelevanceScore { get; set; }
//            public string? Assessment { get; set; }
//            public List<string>? SupportedLevels { get; set; }
//        }
//        private class GeminiResponse
//        {
//            [JsonPropertyName("candidates")]
//            public List<Candidate>? Candidates { get; set; }
//        }
//        private class Candidate
//        {
//            [JsonPropertyName("content")]
//            public Content? Content { get; set; }
//        }
//        private class Content
//        {
//            [JsonPropertyName("parts")]
//            public List<Part>? Parts { get; set; }
//        }
//        private class Part
//        {
//            [JsonPropertyName("text")]
//            public string? Text { get; set; }
//        }
//    }
//}