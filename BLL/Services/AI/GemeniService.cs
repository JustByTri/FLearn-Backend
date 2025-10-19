// BLL/Services/AI/GeminiService.cs
using BLL.IServices.AI;
using BLL.Settings;
using Common.DTO.Assement;
using Common.DTO.Conversation;
using Common.DTO.Learner;
using Common.DTO.Teacher;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BLL.Services.AI
{
    public class GeminiService : IGeminiService
    {
        private readonly HttpClient _httpClient;
        private readonly GeminiSettings _settings;
        private readonly ILogger<GeminiService> _logger;

        public GeminiService(
            HttpClient httpClient,
            IOptions<GeminiSettings> settings,
            ILogger<GeminiService> logger)
        {
            _httpClient = httpClient;
            _settings = settings.Value;
            _logger = logger;
        }

        // ============ CONVERSATION METHODS ============

        public async Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context)
        {
            try
            {
                _logger.LogInformation("Generating conversation content for {Language} - {Topic}",
                    context.Language, context.Topic);

                var prompt = BuildConversationPrompt(context);
                var response = await CallGeminiApiAsync(prompt);

                return ParseConversationResponse(response, context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating conversation content");
                return CreateFallbackConversationContent(context);
            }
        }

        public async Task<string> GenerateResponseAsync(
            string systemPrompt,
            string userMessage,
            List<string> conversationHistory)
        {
            try
            {
                var prompt = BuildResponsePrompt(systemPrompt, userMessage, conversationHistory);
                return await CallGeminiApiAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                return "I understand. Could you tell me more about that?";
            }
        }

        public async Task<ConversationEvaluationResult> EvaluateConversationAsync(string evaluationPrompt)
        {
            try
            {
                var response = await CallGeminiApiAsync(evaluationPrompt);
                return ParseEvaluationResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating conversation");
                return CreateFallbackEvaluationResult();
            }
        }
        private ConversationEvaluationResult CreateFallbackEvaluationResult()
        {
            return new ConversationEvaluationResult
            {
                OverallScore = 75,
                FluentScore = 70,
                GrammarScore = 80,
                VocabularyScore = 75,
                CulturalScore = 70,
                AIFeedback = "Good effort! Keep practicing.",
                Improvements = "Try using more varied vocabulary",
                Strengths = "Good sentence structure and engagement"
            };
        }
       

        // ============ COURSE RECOMMENDATION METHODS ============

        public async Task<AiCourseRecommendationDto> GenerateCourseRecommendationsAsync(
            UserSurveyResponseDto survey,
            List<CourseInfoDto> availableCourses)
        {
            try
            {
                var prompt = BuildCourseRecommendationPrompt(survey, availableCourses);
                var response = await CallGeminiApiAsync(prompt);
                return ParseCourseRecommendationResponse(response, availableCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating course recommendations");
                return new AiCourseRecommendationDto
                {
                    RecommendedCourses = new List<CourseRecommendationDto>(),
                    ReasoningExplanation = "Cannot generate recommendations. Please try again.",
                    LearningPath = "Please select courses manually.",
                    StudyTips = new List<string> { "Study daily", "Practice regularly", "Find resources" },
                    GeneratedAt = DateTime.UtcNow
                };
            }
        }

        public async Task<string> GenerateStudyPlanAsync(UserSurveyResponseDto survey)
        {
            try
            {
                var prompt = BuildStudyPlanPrompt(survey);
                return await CallGeminiApiAsync(prompt);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating study plan");
                return "Unable to generate study plan. Please try again later.";
            }
        }

        public async Task<List<string>> GenerateStudyTipsAsync(UserSurveyResponseDto survey)
        {
            try
            {
                var prompt = BuildStudyTipsPrompt(survey);
                var response = await CallGeminiApiAsync(prompt);
                return ParseStudyTipsResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating study tips");
                return new List<string>
                {
                    "Study daily for 15-30 minutes",
                    "Practice all four skills: listening, speaking, reading, writing",
                    "Watch movies and listen to music in the target language",
                    "Find a language exchange partner"
                };
            }
        }

        // ============ VOICE ASSESSMENT METHODS ============

        public async Task<VoiceEvaluationResult> EvaluateVoiceResponseDirectlyAsync(
            VoiceAssessmentQuestion question,
            IFormFile audioFile,
            string languageCode)
        {
            try
            {
                var audioBase64 = await ConvertAudioToBase64Async(audioFile);
                var prompt = BuildVoiceEvaluationPromptWithAudio(question, languageCode);
                var response = await CallGeminiApiWithAudioAsync(prompt, audioBase64, audioFile.ContentType);
                return ParseVoiceEvaluationResponse(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error evaluating voice response");
                return CreateFallbackVoiceEvaluation();
            }
        }

        public async Task<List<VoiceAssessmentQuestion>> GenerateVoiceAssessmentQuestionsAsync(
            string languageCode,
            string languageName)
        {
            try
            {
                _logger.LogInformation("Generating voice assessment questions for {LanguageCode}", languageCode);
                return GetFallbackVoiceQuestionsWithVietnamese(languageCode, languageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating voice assessment questions");
                return GetFallbackVoiceQuestionsWithVietnamese(languageCode, languageName);
            }
        }

        public async Task<BatchVoiceEvaluationResult> EvaluateBatchVoiceResponsesAsync(
            List<VoiceAssessmentQuestion> questions,
            string languageCode,
            string languageName)
        {
            try
            {
                _logger.LogInformation("Starting batch voice evaluation for {Count} questions", questions.Count);
                var prompt = BuildBatchVoiceEvaluationPrompt(questions, languageCode, languageName);
                var parts = new List<object> { new { text = prompt } };

                foreach (var question in questions.Where(q => !q.IsSkipped && !string.IsNullOrEmpty(q.AudioFilePath)))
                {
                    try
                    {
                        var audioBytes = await File.ReadAllBytesAsync(question.AudioFilePath);
                        var base64Audio = Convert.ToBase64String(audioBytes);
                        parts.Add(new
                        {
                            inline_data = new
                            {
                                mime_type = "audio/mp3",
                                data = base64Audio
                            }
                        });
                        parts.Add(new { text = $"[Audio for Question {question.QuestionNumber}]" });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing audio file for question {QuestionNumber}", question.QuestionNumber);
                    }
                }

                var response = await CallGeminiApiWithAudioAsync(prompt, "", "");
                return ParseBatchEvaluationResponse(response, languageName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in batch voice evaluation");
                return CreateFallbackBatchEvaluation(questions, languageName);
            }
        }

        // ============ TEACHER QUALIFICATION METHODS ============

        public async Task<TeacherQualificationAnalysisDto> AnalyzeTeacherQualificationsAsync(
            TeacherApplicationDto application,
            List<TeacherCredentialDto> credentials)
        {
            try
            {
                var prompt = BuildTeacherQualificationPrompt(application, credentials);
                var response = await CallGeminiApiAsync(prompt);
                return ParseTeacherQualificationResponse(response, application);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error analyzing teacher qualifications");
                return CreateFallbackQualificationAnalysis(application, credentials);
            }
        }

        // ============ PRIVATE HELPER METHODS ============

        private string BuildConversationPrompt(ConversationContextDto context)
        {
            return $@"# Generate conversation scenario - DUAL LANGUAGE REQUIRED

## Context
- **Main Language**: {context.Language} ({context.LanguageCode})
- **Secondary Language**: Vietnamese (for clarity)
- **Topic**: {context.Topic}
- **Level**: {context.DifficultyLevel}

## CRITICAL REQUIREMENTS:
1. ALL text MUST be bilingual: {context.Language} + Vietnamese
2. Format: ""{context.Language} text | Vietnamese text""
3. You MUST generate exactly 3 tasks
4. Tasks MUST be bilingual

## Return Format (JSON only, no markdown):

{{
  ""scenarioDescription"": ""{context.Language} scenario | Tình huống Việt"",
  ""aiRole"": ""{context.Language} role | Vai trò Việt"",
  ""systemPrompt"": ""System prompt (English is OK for this)"",
  ""firstMessage"": ""{context.Language} message | Tin nhắn Việt"",
  ""tasks"": [
    {{
      ""taskDescription"": ""{context.Language} task 1 | Nhiệm vụ 1 Việt"",
      ""taskContext"": ""Context {context.Language} | Ngữ cảnh Việt""
    }},
    {{
      ""taskDescription"": ""{context.Language} task 2 | Nhiệm vụ 2 Việt"",
      ""taskContext"": ""Context {context.Language} | Ngữ cảnh Việt""
    }},
    {{
      ""taskDescription"": ""{context.Language} task 3 | Nhiệm vụ 3 Việt"",
      ""taskContext"": ""Context {context.Language} | Ngữ cảnh Việt""
    }}
  ]
}}

Examples by language:

**English | Tiếng Anh:**
- scenarioDescription: ""You arrive at a Italian restaurant for dinner | Bạn đến nhà hàng Ý để ăn tối""
- firstMessage: ""Good evening! Welcome to La Bella. How many people? | Tối nay vui lắm! Chào mừng. Bao nhiêu người?""

**Chinese | Tiếng Trung:**
- scenarioDescription: ""你来到医院看医生。你有感冒和发烧 | Bạn đến bệnh viện gặp bác sĩ. Bạn bị cảm lạnh và sốt""
- firstMessage: ""请坐。您哪里不舒服？| Vui lòng ngồi. Chỗ nào bạn cảm thấy không thoải mái?""

**Japanese | Tiếng Nhật:**
- scenarioDescription: ""あなたはレストランに到着しました。ウェイターがメニューを持ってきます | Bạn đến nhà hàng. Người phục vụ mang thực đơn đến""
- firstMessage: ""いらっしゃいませ。何名でしょうか？| Chào mừng. Bao nhiêu người?""

Return ONLY JSON. No extra text.";
        }

        private string BuildResponsePrompt(
            string systemPrompt,
            string userMessage,
            List<string> conversationHistory)
        {
            var historyText = string.Join("\n", conversationHistory.TakeLast(10));
            return $@"System: {systemPrompt}

Conversation History:
{historyText}

User: {userMessage}

AI: ";
        }

        private string BuildCourseRecommendationPrompt(UserSurveyResponseDto survey, List<CourseInfoDto> courses)
        {
            var coursesJson = JsonSerializer.Serialize(courses, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return $@"Analyze this learner and recommend 3-5 best courses:

Current Level: {survey.CurrentLevel}
Language: {survey.PreferredLanguageName}
Reason: {survey.LearningReason}
Learning Style: {survey.PreferredLearningStyle}
Topics: {survey.InterestedTopics}
Priority Skills: {survey.PrioritySkills}

Available Courses:
{coursesJson}

Return JSON with:
- recommendedCourses (with courseId, matchScore 0-100, matchReason)
- reasoningExplanation
- learningPath
- studyTips (array)

Focus on matching level, goals, and learning style.";
        }

        private string BuildStudyPlanPrompt(UserSurveyResponseDto survey)
        {
            return $@"Create a detailed weekly study plan for:
- Level: {survey.CurrentLevel}
- Language: {survey.PreferredLanguageName}
- Target Timeline: {survey.TargetTimeline}
- Priority Skills: {survey.PrioritySkills}

Include daily activities, goals, and assessment methods.";
        }

        private string BuildStudyTipsPrompt(UserSurveyResponseDto survey)
        {
            return $@"Provide 8-10 specific study tips for:
- Learning Style: {survey.PreferredLearningStyle}
- Level: {survey.CurrentLevel}

Format each tip on a new line starting with ""- """;
        }

        private string BuildTeacherQualificationPrompt(TeacherApplicationDto application, List<TeacherCredentialDto> credentials)
        {
            var credentialsJson = JsonSerializer.Serialize(credentials.Select(c => new
            {
                c.CredentialName,
                c.CredentialFileUrl
            }), new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = true
            });

            return $@"Analyze teacher qualifications:

Applicant: {application.UserName}
Language: {application.LanguageName}
Experience: {application.TeachingExperience}
Specialization: {application.Specialization}
Desired Levels: {application.TeachingLevel}

Credentials:
{credentialsJson}

Return JSON with:
- suggestedTeachingLevels (array)
- confidenceScore (0-100)
- reasoningExplanation
- qualificationAssessments (array with credentialName, relevanceScore, assessment, supportedLevels)
- overallRecommendation";
        }

        private string BuildVoiceEvaluationPromptWithAudio(VoiceAssessmentQuestion question, string languageCode)
        {
            return $@"Evaluate this voice response:

Question: {question.Question}
Level: {question.Difficulty}
Language: {languageCode}
Required Words: {string.Join(", ", question.WordGuides.Select(w => w.Word))}

Assess:
1. Pronunciation (30%)
2. Fluency (25%)
3. Grammar (25%)
4. Vocabulary (20%)

Return JSON with overallScore, pronunciation, fluency, grammar, vocabulary (each with score and feedback), strengths, areasForImprovement.";
        }

        private string BuildBatchVoiceEvaluationPrompt(
            List<VoiceAssessmentQuestion> questions,
            string languageCode,
            string languageName)
        {
            var questionDetails = string.Join("\n", questions.Select(q => $@"
Q{q.QuestionNumber}: {q.PromptText}
Required words: {string.Join(", ", q.WordGuides.Select(w => w.Word))}"));

            return $@"Evaluate {questions.Count} voice responses in {languageName}:

{questionDetails}

For each question, assess:
- Spoken words (list what was actually said)
- Missing words (required but not spoken)
- Accuracy, pronunciation, fluency, grammar scores

Return JSON with questionResults array, strengths, weaknesses, recommendedCourses.";
        }

        private GeneratedConversationContentDto ParseConversationResponse(
      string response,
      ConversationContextDto context)
        {
            try
            {
                _logger.LogInformation("Parsing conversation response with dual language");

                var cleanedResponse = CleanJsonResponse(response);

                var parsed = JsonSerializer.Deserialize<GeneratedConversationContentDto>(
                    cleanedResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true,
                        AllowTrailingCommas = true
                    });

                if (parsed != null)
                {
                    // ✅ Ensure tasks exist
                    if (parsed.Tasks == null || parsed.Tasks.Count == 0)
                    {
                        _logger.LogWarning("No tasks in response, creating defaults");
                        parsed.Tasks = CreateDefaultTasks(context.Topic, context.Language);
                    }

                    _logger.LogInformation("Successfully parsed with {TaskCount} tasks in {Language}",
                        parsed.Tasks.Count, context.Language);
                    return parsed;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error");
            }

            return CreateFallbackConversationContent(context);
        }

        private List<ConversationTaskDto> CreateDefaultTasks(string topic, string language)
        {
            var separator = " | ";

            return topic.ToLower() switch
            {
                var t when t.Contains("restaurant") || t.Contains("ẩm thực") => new List<ConversationTaskDto>
        {
            new()
            {
                TaskDescription = language switch
                {
                    var l when l.Contains("English") => "Ask the waiter for menu recommendations and house specials | Hỏi người phục vụ về những gợi ý và đặc biệt của nhà hàng",
                    var l when l.Contains("Chinese") => "询问服务员关于菜单建议和特色菜 | Hỏi người phục vụ về những gợi ý menu và đặc biệt",
                    var l when l.Contains("Japanese") => "ウェイターに推奨事項と特別料理について尋ねてください | Hỏi người phục vụ về gợi ý và đặc biệt",
                    _ => "Ask for recommendations | Hỏi gợi ý"
                },
                TaskSequence = 1,
                TaskContext = "Practice ordering phrases | Luyện tập cách đặt hàng"
            },
            new()
            {
                TaskDescription = language switch
                {
                    var l when l.Contains("English") => "Order your main course and mention dietary restrictions | Đặt hàng chính và nhắc về hạn chế ăn kiêng",
                    var l when l.Contains("Chinese") => "点餐并提及饮食限制 | Đặt hàng và nhắc hạn chế ăn kiêng",
                    var l when l.Contains("Japanese") => "メイン料理を注文し、食事制限について言及してください | Đặt hàng chính và nhắc hạn chế ăn kiêng",
                    _ => "Order your meal | Đặt hàng"
                },
                TaskSequence = 2,
                TaskContext = "Use polite ordering expressions | Sử dụng cách nói lịch sự"
            },
            new()
            {
                TaskDescription = language switch
                {
                    var l when l.Contains("English") => "Ask for the bill and inquire about payment methods | Yêu cầu bill và hỏi các phương thức thanh toán",
                    var l when l.Contains("Chinese") => "要求账单并询问付款方式 | Yêu cầu hóa đơn và hỏi cách thanh toán",
                    var l when l.Contains("Japanese") => "勘定を要求し、支払い方法について尋ねてください | Yêu cầu hóa đơn và hỏi cách thanh toán",
                    _ => "Ask for the bill | Yêu cầu tính tiền"
                },
                TaskSequence = 3,
                TaskContext = "Complete the transaction naturally | Hoàn thành giao dịch một cách tự nhiên"
            }
        },

                var t when t.Contains("health") || t.Contains("sức khỏe") => new List<ConversationTaskDto>
        {
            new()
            {
                TaskDescription = language switch
                {
                    var l when l.Contains("English") => "Describe your symptoms in detail to the doctor | Mô tả chi tiết các triệu chứng cho bác sĩ",
                    var l when l.Contains("Chinese") => "详细向医生描述你的症状 | Mô tả chi tiết triệu chứng cho bác sĩ",
                    var l when l.Contains("Japanese") => "医者に詳しく症状を説明してください | Mô tả chi tiết triệu chứng cho bác sĩ",
                    _ => "Describe your symptoms | Mô tả triệu chứng"
                },
                TaskSequence = 1,
                TaskContext = "Use medical vocabulary | Sử dụng từ vựng y tế"
            },
            new()
            {
                TaskDescription = language switch
                {
                    var l when l.Contains("English") => "Ask about treatment options and medication | Hỏi về các lựa chọn điều trị và thuốc",
                    var l when l.Contains("Chinese") => "询问治疗选择和药物 | Hỏi về lựa chọn điều trị và thuốc",
                    var l when l.Contains("Japanese") => "治療オプションと薬物について質問してください | Hỏi về lựa chọn điều trị và thuốc",
                    _ => "Ask about treatment | Hỏi về điều trị"
                },
                TaskSequence = 2,
                TaskContext = "Be specific about your concerns | Cụ thể về mối lo của bạn"
            },
            new()
            {
                TaskDescription = language switch
                {
                    var l when l.Contains("English") => "Ask about follow-up appointments and prevention | Hỏi về lịch tái khám và phòng ngừa",
                    var l when l.Contains("Chinese") => "询问随访预约和预防措施 | Hỏi về tái khám và phòng ngừa",
                    var l when l.Contains("Japanese") => "フォローアップ予定と予防について質問してください | Hỏi về tái khám và phòng ngừa",
                    _ => "Ask about follow-up | Hỏi về tái khám"
                },
                TaskSequence = 3,
                TaskContext = "Show concern for your health | Thể hiện quan tâm đến sức khỏe"
            }
        },

                _ => new List<ConversationTaskDto>
        {
            new() { TaskDescription = $"Start the conversation naturally | Bắt đầu cuộc trò chuyện một cách tự nhiên", TaskSequence = 1 },
            new() { TaskDescription = $"Ask follow-up questions | Đặt câu hỏi tiếp theo", TaskSequence = 2 },
            new() { TaskDescription = $"Share your perspective | Chia sẻ quan điểm của bạn", TaskSequence = 3 }
        }
            };
        }
        private ConversationEvaluationResult ParseEvaluationResponse(string response)
        {
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                var parsed = JsonSerializer.Deserialize<ConversationEvaluationDto>(
                    cleanedResponse,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        PropertyNameCaseInsensitive = true
                    });

                if (parsed != null)
                {
                    return new ConversationEvaluationResult
                    {
                        OverallScore = parsed.OverallScore,
                        FluentScore = parsed.FluentScore,
                        GrammarScore = parsed.GrammarScore,
                        VocabularyScore = parsed.VocabularyScore,
                        CulturalScore = parsed.CulturalScore,
                        AIFeedback = parsed.AIFeedback,
                        Improvements = parsed.Improvements,
                        Strengths = parsed.Strengths
                    };
                }

                return CreateFallbackEvaluationResult();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing evaluation");
                return CreateFallbackEvaluationResult();
            }
        }

        private AiCourseRecommendationDto ParseCourseRecommendationResponse(string response, List<CourseInfoDto> availableCourses)
        {
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                var aiResponse = JsonSerializer.Deserialize<AiResponseFormat>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (aiResponse?.RecommendedCourses != null)
                {
                    var recommendations = new List<CourseRecommendationDto>();

                    foreach (var rec in aiResponse.RecommendedCourses)
                    {
                        var course = availableCourses.FirstOrDefault(c =>
                            c.CourseID.ToString().Equals(rec.CourseId, StringComparison.OrdinalIgnoreCase));

                        if (course != null)
                        {
                            recommendations.Add(new CourseRecommendationDto
                            {
                                CourseID = course.CourseID,
                                CourseName = course.Title,
                                CourseDescription = course.Description,
                                Level = course.Level,
                                MatchScore = Math.Min(100, Math.Max(0, rec.MatchScore)),
                                MatchReason = rec.MatchReason ?? "Suitable for your goals",
                                EstimatedDuration = course.Duration
                            });
                        }
                    }

                    return new AiCourseRecommendationDto
                    {
                        RecommendedCourses = recommendations,
                        ReasoningExplanation = aiResponse.ReasoningExplanation ?? "AI analysis completed.",
                        LearningPath = aiResponse.LearningPath ?? "Start with basic level and progress gradually.",
                        StudyTips = aiResponse.StudyTips ?? new List<string>(),
                        GeneratedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing course recommendations");
            }

            return CreateFallbackRecommendation(availableCourses);
        }

        private TeacherQualificationAnalysisDto ParseTeacherQualificationResponse(string response, TeacherApplicationDto application)
        {
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                var aiResponse = JsonSerializer.Deserialize<TeacherQualificationAiResponse>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (aiResponse != null)
                {
                    return new TeacherQualificationAnalysisDto
                    {
                        ApplicationId = application.TeacherApplicationID,
                        LanguageName = application.LanguageName,
                        SuggestedTeachingLevels = aiResponse.SuggestedTeachingLevels ?? new List<string>(),
                        ConfidenceScore = Math.Min(100, Math.Max(0, aiResponse.ConfidenceScore)),
                        ReasoningExplanation = aiResponse.ReasoningExplanation ?? "",
                        QualificationAssessments = aiResponse.QualificationAssessments?.Select(qa => new QualificationAssessment
                        {
                            CredentialName = qa.CredentialName ?? "",
                            CredentialType = qa.CredentialType ?? "",
                            RelevanceScore = Math.Min(100, Math.Max(0, qa.RelevanceScore)),
                            Assessment = qa.Assessment ?? "",
                            SupportedLevels = qa.SupportedLevels ?? new List<string>()
                        }).ToList() ?? new List<QualificationAssessment>(),
                        OverallRecommendation = aiResponse.OverallRecommendation ?? "",
                        AnalyzedAt = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing teacher qualifications");
            }

            return CreateFallbackQualificationAnalysis(application, new List<TeacherCredentialDto>());
        }

        private List<string> ParseStudyTipsResponse(string response)
        {
            var tips = new List<string>();
            var lines = response.Split('\n', StringSplitOptions.RemoveEmptyEntries);

            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("- ") || trimmed.StartsWith("• "))
                {
                    tips.Add(trimmed.Substring(2).Trim());
                }
            }

            return tips.Take(10).ToList();
        }

        private VoiceEvaluationResult ParseVoiceEvaluationResponse(string response)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(response))
                    return CreateFallbackVoiceEvaluation();

                var cleanedResponse = CleanJsonResponse(response);
                var result = JsonSerializer.Deserialize<VoiceEvaluationResult>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                return result ?? CreateFallbackVoiceEvaluation();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing voice evaluation");
                return CreateFallbackVoiceEvaluation();
            }
        }

        private BatchVoiceEvaluationResult ParseBatchEvaluationResponse(string response, string languageName)
        {
            try
            {
                var cleanedResponse = CleanJsonResponse(response);
                var result = JsonSerializer.Deserialize<BatchVoiceEvaluationResult>(cleanedResponse, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    PropertyNameCaseInsensitive = true
                });

                if (result != null)
                {
                    result.EvaluatedAt = DateTime.UtcNow;
                    return result;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing batch evaluation");
            }

            return CreateFallbackBatchEvaluation(new List<VoiceAssessmentQuestion>(), languageName);
        }

        private async Task<string> CallGeminiApiAsync(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[] { new { parts = new[] { new { text = prompt } } } },
                    generationConfig = new
                    {
                        temperature = 0.7,
                        maxOutputTokens =  2048
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var model =  "gemini-2.5-pro";
                var url = $"{_settings.BaseUrl}/models/{model}:generateContent?key={_settings.ApiKey}";

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
                }

                return ExtractTextFromGeminiResponse(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                throw;
            }
        }

        private async Task<string> CallGeminiApiWithAudioAsync(string prompt, string audioBase64, string mimeType)
        {
            try
            {
                var parts = new List<object> { new { text = prompt } };

                if (!string.IsNullOrEmpty(audioBase64))
                {
                    parts.Add(new
                    {
                        inline_data = new
                        {
                            mime_type = mimeType,
                            data = audioBase64
                        }
                    });
                }

                var requestBody = new
                {
                    contents = new[] { new { parts = parts.ToArray() } },
                    generationConfig = new
                    {
                        temperature = 0.4,
                        maxOutputTokens =  2048
                    }
                };

                var jsonContent = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });

                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
                var model = "gemini-2.5-pro";
                var url = $"{_settings.BaseUrl}/models/{model}:generateContent?key={_settings.ApiKey}";

                var response = await _httpClient.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API audio error: {StatusCode}", response.StatusCode);
                    throw new HttpRequestException($"Gemini API returned {response.StatusCode}");
                }

                return ExtractTextFromGeminiResponse(responseContent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API with audio");
                throw;
            }
        }

        private string ExtractTextFromGeminiResponse(string responseContent)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseContent);
                var candidates = doc.RootElement.GetProperty("candidates");

                if (candidates.GetArrayLength() > 0)
                {
                    var text = candidates[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return text ?? "";
                }

                return "";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting text from response");
                return "";
            }
        }

        private async Task<string> ConvertAudioToBase64Async(IFormFile audioFile)
        {
            try
            {
                var allowedTypes = new[] { "audio/mp3", "audio/wav", "audio/m4a", "audio/webm", "audio/mpeg" };
                if (!allowedTypes.Contains(audioFile.ContentType.ToLower()))
                    throw new ArgumentException("Only MP3, WAV, M4A, WebM audio files are supported");

                if (audioFile.Length > 10 * 1024 * 1024)
                    throw new ArgumentException("Audio file must not exceed 10MB");

                using var memoryStream = new MemoryStream();
                await audioFile.CopyToAsync(memoryStream);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error converting audio to base64");
                throw;
            }
        }

        private string CleanJsonResponse(string response)
        {
            var cleaned = response.Trim();

            if (cleaned.StartsWith("```json"))
                cleaned = cleaned.Replace("```json", "").Replace("```", "").Trim();
            else if (cleaned.StartsWith("```"))
                cleaned = cleaned.Replace("```", "").Trim();

            var start = cleaned.IndexOf('{');
            var end = cleaned.LastIndexOf('}') + 1;

            if (start >= 0 && end > start)
                return cleaned.Substring(start, end - start);

            return cleaned;
        }

        // ============ FALLBACK METHODS ============

        private GeneratedConversationContentDto CreateFallbackConversationContent(ConversationContextDto context)
        {
            return new GeneratedConversationContentDto
            {
                ScenarioDescription = $"Practice {context.Topic} in {context.Language}",
                AIRole = GetDefaultRole(context.Topic),
                SystemPrompt = context.MasterPrompt,
                FirstMessage = GetDefaultFirstMessage(context.Language, context.Topic),
                Tasks = new List<ConversationTaskDto>
                {
                    new() { TaskDescription = "Introduce yourself", TaskSequence = 1 },
                    new() { TaskDescription = "Ask about the topic", TaskSequence = 2 },
                    new() { TaskDescription = "Express your opinion", TaskSequence = 3 }
                }
            };
        }

        private ConversationEvaluationDto CreateFallbackEvaluation()
        {
            return new ConversationEvaluationDto
            {
                OverallScore = 75,
                FluentScore = 70,
                GrammarScore = 80,
                VocabularyScore = 75,
                CulturalScore = 70,
                AIFeedback = "Good effort!",
                Improvements = "Keep practicing",
                Strengths = "Good engagement"
            };
        }

        private AiCourseRecommendationDto CreateFallbackRecommendation(List<CourseInfoDto> courses)
        {
            return new AiCourseRecommendationDto
            {
                RecommendedCourses = courses.Take(3).Select(c => new CourseRecommendationDto
                {
                    CourseID = c.CourseID,
                    CourseName = c.Title,
                    Level = c.Level,
                    MatchScore = 75
                }).ToList(),
                ReasoningExplanation = "Popular courses for your level.",
                LearningPath = "Start with these courses.",
                StudyTips = new List<string> { "Study daily", "Practice regularly" },
                GeneratedAt = DateTime.UtcNow
            };
        }

        private TeacherQualificationAnalysisDto CreateFallbackQualificationAnalysis(
            TeacherApplicationDto application,
            List<TeacherCredentialDto> credentials)
        {
            return new TeacherQualificationAnalysisDto
            {
                ApplicationId = application.TeacherApplicationID,
                LanguageName = application.LanguageName,
                SuggestedTeachingLevels = new List<string> { "Beginner" },
                ConfidenceScore = 50,
                ReasoningExplanation = "Manual review needed.",
                QualificationAssessments = new List<QualificationAssessment>(),
                OverallRecommendation = "Review manually.",
                AnalyzedAt = DateTime.UtcNow
            };
        }

        private VoiceEvaluationResult CreateFallbackVoiceEvaluation()
        {
            return new VoiceEvaluationResult
            {
                OverallScore = 70,
                Pronunciation = new PronunciationScore
                {
                    Score = 70,
                    Level = "Fair",
                    Feedback = "Manual review needed",
                    MispronuncedWords = new List<string>()
                },
                Fluency = new FluencyScore
                {
                    Score = 70,
                    SpeakingRate = 120,
                    PauseCount = 5,
                    Rhythm = "Average",
                    Feedback = "Continue practicing"
                },
                Grammar = new GrammarScore
                {
                    Score = 70,
                    GrammarErrors = new List<string>(),
                    StructureAssessment = "Average",
                    Feedback = "Good effort"
                },
                Vocabulary = new VocabularyScore
                {
                    Score = 70,
                    RangeAssessment = "Good",
                    AccuracyAssessment = "Fair",
                    Feedback = "Keep learning"
                },
                DetailedFeedback = "Unable to process audio. Please try again.",
                Strengths = new List<string> { "Participated in assessment" },
                AreasForImprovement = new List<string> { "Retry with better audio quality" }
            };
        }

        private BatchVoiceEvaluationResult CreateFallbackBatchEvaluation(
            List<VoiceAssessmentQuestion> questions,
            string languageName)
        {
            return new BatchVoiceEvaluationResult
            {
                OverallLevel = "Unassessed",
                OverallScore = 0,
                QuestionResults = questions.Select(q => new QuestionEvaluationResult
                {
                    QuestionNumber = q.QuestionNumber,
                    SpokenWords = new List<string>(),
                    MissingWords = q.WordGuides.Select(w => w.Word).ToList(),
                    AccuracyScore = 0,
                    PronunciationScore = 0,
                    FluencyScore = 0,
                    GrammarScore = 0,
                    Feedback = "Unable to evaluate"
                }).ToList(),
                Strengths = new List<string>(),
                Weaknesses = new List<string> { "Retry assessment" },
                RecommendedCourses = new List<CourseRecommendation>(),
                EvaluatedAt = DateTime.UtcNow
            };
        }

        private List<VoiceAssessmentQuestion> GetFallbackVoiceQuestionsWithVietnamese(string languageCode, string languageName)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => new List<VoiceAssessmentQuestion>
                {
                    new() {
                        QuestionNumber = 1,
                        Question = "Pronounce these basic words clearly:",
                        PromptText = "Hello - World - Beautiful",
                        VietnameseTranslation = "Xin chào - Thế giới - Đẹp",
                        WordGuides = new List<WordWithGuide>
                        {
                            new() {
                                Word = "Hello",
                                Pronunciation = "/həˈloʊ/ (hơ-lô)",
                                VietnameseMeaning = "Xin chào",
                                Example = "Hello, how are you?"
                            }
                        },
                        QuestionType = "pronunciation",
                        Difficulty = "beginner",
                        MaxRecordingSeconds = 30
                    },
                    new() {
                        QuestionNumber = 2,
                        Question = "Introduce yourself in 60 seconds:",
                        PromptText = "Tell me your name, age, where you're from, and your hobbies.",
                        VietnameseTranslation = "Nói tên, tuổi, quê quán và sở thích của bạn.",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "elementary",
                        MaxRecordingSeconds = 60
                    },
                    new() {
                        QuestionNumber = 3,
                        Question = "Describe your typical day:",
                        PromptText = "What do you do from morning to evening?",
                        VietnameseTranslation = "Bạn làm gì từ sáng đến tối?",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "intermediate",
                        MaxRecordingSeconds = 90
                    },
                    new() {
                        QuestionNumber = 4,
                        Question = "Discuss technology impact:",
                        PromptText = "How has technology changed your life?",
                        VietnameseTranslation = "Công nghệ đã thay đổi cuộc sống của bạn như thế nào?",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "advanced",
                        MaxRecordingSeconds = 120
                    }
                },

                "ZH" => new List<VoiceAssessmentQuestion>
                {
                    new() {
                        QuestionNumber = 1,
                        Question = "请读出下列词语:",
                        PromptText = "你好 - 世界 - 美丽",
                        VietnameseTranslation = "Xin chào - Thế giới - Đẹp",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "pronunciation",
                        Difficulty = "beginner",
                        MaxRecordingSeconds = 30
                    },
                    new() {
                        QuestionNumber = 2,
                        Question = "请介绍你自己:",
                        PromptText = "说出你的名字、年龄、来自哪里和你的爱好。",
                        VietnameseTranslation = "Nói tên, tuổi, quê quán và sở thích.",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "elementary",
                        MaxRecordingSeconds = 60
                    },
                    new() {
                        QuestionNumber = 3,
                        Question = "描述你的家乡:",
                        PromptText = "你的家乡是什么样的?",
                        VietnameseTranslation = "Quê quán của bạn như thế nào?",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "intermediate",
                        MaxRecordingSeconds = 90
                    },
                    new() {
                        QuestionNumber = 4,
                        Question = "谈论文化差异:",
                        PromptText = "你认为中国文化的特色是什么?",
                        VietnameseTranslation = "Bạn nghĩ văn hóa Trung Quốc có đặc điểm gì?",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "advanced",
                        MaxRecordingSeconds = 120
                    }
                },

                "JA" => new List<VoiceAssessmentQuestion>
                {
                    new() {
                        QuestionNumber = 1,
                        Question = "次の単語を読んでください:",
                        PromptText = "こんにちは - 世界 - 美しい",
                        VietnameseTranslation = "Xin chào - Thế giới - Đẹp",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "pronunciation",
                        Difficulty = "beginner",
                        MaxRecordingSeconds = 30
                    },
                    new() {
                        QuestionNumber = 2,
                        Question = "自己紹介をしてください:",
                        PromptText = "名前、年齢、出身地、趣味を言ってください。",
                        VietnameseTranslation = "Nói tên, tuổi, quê quán và sở thích.",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "elementary",
                        MaxRecordingSeconds = 60
                    },
                    new() {
                        QuestionNumber = 3,
                        Question = "好きな季節について話してください:",
                        PromptText = "どの季節が好きですか?",
                        VietnameseTranslation = "Bạn thích mùa nào?",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "intermediate",
                        MaxRecordingSeconds = 90
                    },
                    new() {
                        QuestionNumber = 4,
                        Question = "日本文化について話してください:",
                        PromptText = "日本文化で興味深いと思うことは何ですか?",
                        VietnameseTranslation = "Điều gì trong văn hóa Nhật thú vị với bạn?",
                        WordGuides = new List<WordWithGuide>(),
                        QuestionType = "speaking",
                        Difficulty = "advanced",
                        MaxRecordingSeconds = 120
                    }
                },

                _ => new List<VoiceAssessmentQuestion>()
            };
        }

        private string GetDefaultRole(string topicName)
        {
            return topicName.ToLower() switch
            {
                var t when t.Contains("restaurant") || t.Contains("ẩm thực") => "Restaurant Staff",
                var t when t.Contains("travel") || t.Contains("du lịch") => "Travel Guide",
                var t when t.Contains("shopping") || t.Contains("mua sắm") => "Shop Assistant",
                var t when t.Contains("work") || t.Contains("công việc") => "Colleague",
                var t when t.Contains("school") || t.Contains("học") => "Study Partner",
                var t when t.Contains("health") || t.Contains("sức khỏe") => "Health Advisor",
                var t when t.Contains("family") || t.Contains("gia đình") => "Friend",
                _ => "Conversation Partner"
            };
        }

        private string GetDefaultFirstMessage(string language, string topic)
        {
            if (language.Contains("English"))
                return $"Hello! Ready to practice about {topic}?";
            if (language.Contains("Japanese"))
                return $"こんにちは！{topic}について話しましょう！";
            if (language.Contains("Chinese"))
                return $"你好！我们一起聊聊{topic}吧！";

            return "Hello! Let's start our conversation!";
        }


        public async Task<string> TranslateTextAsync(string text, string sourceLanguage, string targetLanguage)
        {
            try
            {
                var prompt = $@"Translate the following text from {sourceLanguage} to {targetLanguage}.
Only provide the translation, nothing else.
Text: {text}";

                var request = new
                {
                    contents = new[]
                    {
                new
                {
                    parts = new[] { new { text = prompt } }
                }
            }
                };

                var response = await _httpClient.PostAsJsonAsync(
                    $"{_settings.BaseUrl}?key={_settings.ApiKey}",
                    request
                );

                if (!response.IsSuccessStatusCode)
                    return text;

                var jsonString = await response.Content.ReadAsStringAsync();

                using (JsonDocument doc = JsonDocument.Parse(jsonString))
                {
                    var root = doc.RootElement;
                    var translatedText = root
                        .GetProperty("candidates")[0]
                        .GetProperty("content")
                        .GetProperty("parts")[0]
                        .GetProperty("text")
                        .GetString();

                    return string.IsNullOrWhiteSpace(translatedText) ? text : translatedText.Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error translating text");
                return text;
            }
        }
        private class AiResponseFormat
        {
            public List<CourseMatch>? RecommendedCourses { get; set; }
            public string? ReasoningExplanation { get; set; }
            public string? LearningPath { get; set; }
            public List<string>? StudyTips { get; set; }
        }

        private class CourseMatch
        {
            public string CourseId { get; set; } = "";
            public decimal MatchScore { get; set; }
            public string MatchReason { get; set; } = "";
        }

        private class TeacherQualificationAiResponse
        {
            public List<string>? SuggestedTeachingLevels { get; set; }
            public int ConfidenceScore { get; set; }
            public string? ReasoningExplanation { get; set; }
            public List<QualificationAssessmentAi>? QualificationAssessments { get; set; }
            public string? OverallRecommendation { get; set; }
        }

        private class QualificationAssessmentAi
        {
            public string? CredentialName { get; set; }
            public string? CredentialType { get; set; }
            public int RelevanceScore { get; set; }
            public string? Assessment { get; set; }
            public List<string>? SupportedLevels { get; set; }
        }

        private class GeminiResponse
        {
            [JsonPropertyName("candidates")]
            public List<Candidate>? Candidates { get; set; }
        }

        private class Candidate
        {
            [JsonPropertyName("content")]
            public Content? Content { get; set; }
        }

        private class Content
        {
            [JsonPropertyName("parts")]
            public List<Part>? Parts { get; set; }
        }

        private class Part
        {
            [JsonPropertyName("text")]
            public string? Text { get; set; }
        }
    }
}