using BLL.Hubs;
using BLL.IServices.AI;
using BLL.IServices.Coversation;
using Common.DTO.Conversation;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Services
{
    public class ConversationPartnerService : IConversationPartnerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<ConversationPartnerService> _logger;
        private readonly IHubContext<ConversationHub> _hubContext;

        public ConversationPartnerService(
            IUnitOfWork unitOfWork,
            IGeminiService geminiService,
            ILogger<ConversationPartnerService> logger,
            IHubContext<ConversationHub> hubContext)
        {
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
            _logger = logger;
            _hubContext = hubContext;
        }

        public async Task<List<ConversationLanguageDto>> GetAvailableLanguagesAsync()
        {
            try
            {
                var languages = await _unitOfWork.Languages.GetAllAsync();
                var result = new List<ConversationLanguageDto>();

                foreach (var language in languages)
                {
                    var levels = await GetLevelsByLanguageAsync(language.LanguageID);

                    result.Add(new ConversationLanguageDto
                    {
                        LanguageId = language.LanguageID,
                        LanguageName = language.LanguageName,
                        LanguageCode = language.LanguageCode,
                        AvailableLevels = levels
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available languages");
                return new List<ConversationLanguageDto>();
            }
        }

        public async Task<List<ConversationTopicDto>> GetAvailableTopicsAsync()
        {
            try
            {
                var topics = await _unitOfWork.Topics.GetAllAsync();

                return topics.Where(t => t.Status).Select(t => new ConversationTopicDto
                {
                    TopicId = t.TopicID,
                    Name = t.Name,
                    Description = t.Description,
                
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available topics");
                return new List<ConversationTopicDto>();
            }
        }

        public async Task<List<string>> GetLevelsByLanguageAsync(Guid languageId)
        {
            try
            {
                var languageLevels = await _unitOfWork.LanguageLevels.GetAllAsync();

                return languageLevels
                    .Where(ll => ll.LanguageID == languageId)
                    .OrderBy(ll => ll.Position)
                    .Select(ll => ll.LevelName)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting levels for language {LanguageId}", languageId);
                return new List<string>();
            }
        }

        public async Task<ConversationSessionDto> StartConversationAsync(Guid userId, StartConversationRequestDto request)
        {
            try
            {
                var language = await _unitOfWork.Languages.GetByIdAsync(request.LanguageId);
                var topic = await _unitOfWork.Topics.GetByIdAsync(request.TopicId);

                if (language == null || topic == null)
                    throw new ArgumentException("Language or topic not found");

                // Lấy global prompt đang active
                var activeGlobalPrompt = await _unitOfWork.GlobalConversationPrompts.GetActiveDefaultPromptAsync();
                if (activeGlobalPrompt == null)
                    throw new InvalidOperationException("No active global conversation prompt found");

                // Chuẩn bị context để gửi cho AI
                var conversationContext = new ConversationContextDto
                {
                    Language = language.LanguageName,
                    LanguageCode = language.LanguageCode,
                    Topic = topic.Name,
                    TopicDescription = topic.Description,
                    DifficultyLevel = request.DifficultyLevel,
                    MasterPrompt = activeGlobalPrompt.MasterPromptTemplate,
                    ScenarioGuidelines = activeGlobalPrompt.ScenarioGuidelines ?? "",
                    RoleplayInstructions = activeGlobalPrompt.RoleplayInstructions ?? "",
                    EvaluationCriteria = activeGlobalPrompt.EvaluationCriteria ?? ""
                };

                // Sử dụng AI để tạo scenario và system prompt cụ thể
                var generatedContent = await GenerateConversationContentAsync(conversationContext);

                // Tạo session mới
                var session = new ConversationSession
                {
                    ConversationSessionID = Guid.NewGuid(),
                    UserId = userId,
                    LanguageId = request.LanguageId,
                    TopicID = request.TopicId,
                    GlobalPromptID = activeGlobalPrompt.GlobalPromptID,
                    DifficultyLevel = request.DifficultyLevel,
                    SessionName = $"{topic.Name} - {request.DifficultyLevel}",
                    GeneratedScenario = generatedContent.ScenarioDescription,
                    AICharacterRole = generatedContent.AIRole,
                    GeneratedSystemPrompt = generatedContent.SystemPrompt,
                    StartedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationSessions.CreateAsync(session);

                // Tạo tin nhắn đầu tiên từ AI
                var firstMessage = new ConversationMessage
                {
                    ConversationMessageID = Guid.NewGuid(),
                    ConversationSessionID = session.ConversationSessionID,
                    Sender = MessageSender.AI,
                    MessageContent = generatedContent.FirstMessage,
                    MessageType = MessageType.Text,
                    SequenceOrder = 1,
                    SentAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationMessages.CreateAsync(firstMessage);

                // Cập nhật usage count
                activeGlobalPrompt.UsageCount++;
                await _unitOfWork.GlobalConversationPrompts.UpdateAsync(activeGlobalPrompt);

                await _unitOfWork.SaveChangesAsync();

                // 🔥 Thông báo real-time qua SignalR
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ConversationStarted", new
                    {
                        sessionId = session.ConversationSessionID,
                        sessionName = session.SessionName,
                        languageName = language.LanguageName,
                        topicName = topic.Name,
                        scenario = generatedContent.ScenarioDescription,
                        startedAt = DateTime.UtcNow
                    });

                return MapToConversationSessionDto(session, new List<ConversationMessage> { firstMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");

                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ConversationError", new
                    {
                        error = "Không thể bắt đầu cuộc trò chuyện",
                        details = ex.Message
                    });

                throw;
            }
        }

        public async Task<ConversationMessageDto> SendMessageAsync(Guid userId, SendMessageRequestDto request)
        {
            try
            {
                var session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(request.SessionId);

                if (session == null || session.UserId != userId)
                    throw new ArgumentException("Session not found or access denied");

                if (session.Status != ConversationSessionStatus.Active)
                    throw new InvalidOperationException("Session is not active");

                var nextSequence = (session.ConversationMessages?.Count ?? 0) + 1;

             
                var userMessage = new ConversationMessage
                {
                    ConversationMessageID = Guid.NewGuid(),
                    ConversationSessionID = request.SessionId,
                    Sender = MessageSender.User,
                    MessageContent = request.MessageContent,
                    MessageType = request.MessageType,
                    AudioUrl = request.AudioUrl,
                    AudioPublicId = request.AudioPublicId,
                    AudioDuration = request.AudioDuration,
                    SequenceOrder = nextSequence,
                    SentAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationMessages.CreateAsync(userMessage);

                // Tạo phản hồi từ AI
                var aiResponse = await GenerateAIResponseAsync(session, request.MessageContent);
                var aiMessage = new ConversationMessage
                {
                    ConversationMessageID = Guid.NewGuid(),
                    ConversationSessionID = request.SessionId,
                    Sender = MessageSender.AI,
                    MessageContent = aiResponse,
                    MessageType = MessageType.Text,
                    SequenceOrder = nextSequence + 1,
                    SentAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationMessages.CreateAsync(aiMessage);

                // Cập nhật session
                session.MessageCount += 2;
                session.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ConversationSessions.UpdateAsync(session);

                await _unitOfWork.SaveChangesAsync();

                // 🔥 Thông báo real-time
                await _hubContext.Clients.Group($"Conversation_{request.SessionId}")
                    .SendAsync("MessageProcessed", new
                    {
                        userMessage = MapToMessageDto(userMessage),
                        aiMessage = MapToMessageDto(aiMessage)
                    });

                return MapToMessageDto(aiMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");

                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("MessageError", new
                    {
                        sessionId = request.SessionId,
                        error = "Không thể gửi tin nhắn",
                        details = ex.Message
                    });

                throw;
            }
        }

        public async Task<ConversationEvaluationDto> EndConversationAsync(Guid userId, Guid sessionId)
        {
            try
            {
                var session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(sessionId);

                if (session == null || session.UserId != userId)
                    throw new ArgumentException("Session not found or access denied");

                if (session.Status != ConversationSessionStatus.Active)
                    throw new InvalidOperationException("Session is not active");

            
                var evaluation = await GenerateEvaluationAsync(session);

           
                session.Status = ConversationSessionStatus.Completed;
                session.EndedAt = DateTime.UtcNow;
                session.OverallScore = evaluation.OverallScore;
                session.FluentScore = evaluation.FluentScore;
                session.GrammarScore = evaluation.GrammarScore;
                session.VocabularyScore = evaluation.VocabularyScore;
                session.CulturalScore = evaluation.CulturalScore;
                session.AIFeedback = evaluation.AIFeedback;
                session.Improvements = evaluation.Improvements;
                session.Strengths = evaluation.Strengths;
                session.Duration = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;

                await _unitOfWork.ConversationSessions.UpdateAsync(session);
                await _unitOfWork.SaveChangesAsync();

            
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ConversationEvaluated", new
                    {
                        sessionId,
                        evaluation = new
                        {
                            overallScore = evaluation.OverallScore,
                            feedback = evaluation.AIFeedback,
                            strengths = evaluation.Strengths,
                            improvements = evaluation.Improvements,
                            completedAt = DateTime.UtcNow
                        }
                    });

                return evaluation;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending conversation");

                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("EvaluationError", new
                    {
                        sessionId,
                        error = "Không thể đánh giá cuộc trò chuyện"
                    });

                throw;
            }
        }

        public async Task<List<ConversationSessionDto>> GetUserConversationHistoryAsync(Guid userId)
        {
            try
            {
                var sessions = await _unitOfWork.ConversationSessions.GetUserSessionsAsync(userId);
                var result = new List<ConversationSessionDto>();

                foreach (var session in sessions.OrderByDescending(s => s.StartedAt))
                {
                    result.Add(new ConversationSessionDto
                    {
                        SessionId = session.ConversationSessionID,
                        SessionName = session.SessionName,
                        LanguageName = session.Language?.LanguageName ?? "",
                        TopicName = session.Topic?.Name ?? "",
                        DifficultyLevel = session.DifficultyLevel,
                        CharacterRole = session.AICharacterRole ?? "",
                        ScenarioDescription = session.GeneratedScenario ?? "",
                        Status = session.Status,
                        StartedAt = session.StartedAt,
                        OverallScore = session.OverallScore,
                        AIFeedback = session.AIFeedback,
                        Messages = new List<ConversationMessageDto>()
                    });
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history for user {UserId}", userId);
                return new List<ConversationSessionDto>();
            }
        }

        public async Task<ConversationSessionDto?> GetConversationSessionAsync(Guid userId, Guid sessionId)
        {
            try
            {
                var session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(sessionId);

                if (session == null || session.UserId != userId)
                    return null;

                var messages = session.ConversationMessages?
                    .OrderBy(m => m.SequenceOrder)
                    .Select(MapToMessageDto)
                    .ToList() ?? new List<ConversationMessageDto>();

                return new ConversationSessionDto
                {
                    SessionId = session.ConversationSessionID,
                    SessionName = session.SessionName,
                    LanguageName = session.Language?.LanguageName ?? "",
                    TopicName = session.Topic?.Name ?? "",
                    DifficultyLevel = session.DifficultyLevel,
                    CharacterRole = session.AICharacterRole ?? "",
                    ScenarioDescription = session.GeneratedScenario ?? "",
                    Messages = messages,
                    Status = session.Status,
                    StartedAt = session.StartedAt,
                    OverallScore = session.OverallScore,
                    AIFeedback = session.AIFeedback
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation session {SessionId} for user {UserId}", sessionId, userId);
                return null;
            }
        }

        #region Private Methods

        private async Task<GeneratedConversationContentDto> GenerateConversationContentAsync(ConversationContextDto context)
        {
            try
            {
                // Nếu có Gemini service thì dùng AI
                if (_geminiService != null)
                {
                    return await _geminiService.GenerateConversationContentAsync(context);
                }

                // Fallback: tạo content đơn giản
                return new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice {context.Topic} in {context.Language} at {context.DifficultyLevel} level",
                    AIRole = GetDefaultRole(context.Topic),
                    SystemPrompt = context.MasterPrompt.Replace("{LANGUAGE}", context.Language)
                        .Replace("{TOPIC}", context.Topic)
                        .Replace("{DIFFICULTY_LEVEL}", context.DifficultyLevel),
                    FirstMessage = GetDefaultFirstMessage(context.Language, context.Topic, context.DifficultyLevel)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating conversation content");

                // Fallback content
                return new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice conversation about {context.Topic}",
                    AIRole = "Conversation Partner",
                    SystemPrompt = $"You are helping someone practice {context.Language}",
                    FirstMessage = "Hello! Let's start our conversation practice."
                };
            }

        }
        private async Task<string> GenerateAIResponseAsync(ConversationSession session, string userMessage)
        {
            try
            {
                // Sử dụng AI service nếu có
                if (_geminiService != null)
                {
                    var context = new
                    {
                        SystemPrompt = session.GeneratedSystemPrompt,
                        UserMessage = userMessage,
                        ConversationHistory = session.ConversationMessages?
                            .OrderBy(m => m.SequenceOrder)
                            .Select(m => $"{m.Sender}: {m.MessageContent}")
                            .ToList() ?? new List<string>()
                    };

                    return await _geminiService.GenerateResponseAsync(context.SystemPrompt, userMessage, context.ConversationHistory);
                }

                // Fallback: phản hồi đơn giản theo ngôn ngữ
                return GetSimpleResponse(userMessage, session.Language?.LanguageCode ?? "EN");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                return GetDefaultResponse(session.Language?.LanguageCode ?? "EN");
            }
        }

        private string GetSimpleResponse(string userMessage, string languageCode)
        {
            var responses = languageCode.ToUpper() switch
            {
                "EN" => new[]
                {
                    "That's interesting! Can you tell me more about that?",
                    "I understand. What do you think about this situation?",
                    "Great point! How does that make you feel?",
                    "That's a good way to look at it. What would you do differently?",
                    "I see. Can you give me an example?",
                    "That sounds challenging. How did you handle it?",
                    "Interesting perspective! What led you to think that way?",
                    "I appreciate your thoughts on this. What's your next step?"
                },
                "JP" => new[]
                {
                    "それは面白いですね！もう少し詳しく教えてください。",
                    "なるほど、理解しました。この状況についてどう思いますか？",
                    "いいポイントですね！どんな気持ちになりましたか？",
                    "それはいい考え方ですね。違ったらどうしますか？",
                    "そうですね。例を教えてもらえますか？",
                    "それは大変そうですね。どう対処しましたか？",
                    "興味深い視点ですね！そう思ったきっかけは何ですか？",
                    "貴重なご意見をありがとうございます。次はどうしますか？"
                },
                "ZH" => new[]
                {
                    "这很有趣！你能告诉我更多吗？",
                    "我明白了。你对这种情况有什么看法？",
                    "很好的观点！你感觉怎么样？",
                    "这是个好想法。如果不同的话你会怎么做？",
                    "我明白了。你能举个例子吗？",
                    "听起来很有挑战性。你是怎么处理的？",
                    "有趣的观点！是什么让你这样想的？",
                    "感谢你的想法。你下一步打算怎么做？"
                },
                _ => new[]
                {
                    "That's interesting! Can you tell me more?",
                    "I understand. What do you think?",
                    "Great! How do you feel about that?",
                    "I see. Can you explain more?",
                    "That sounds good. What's next?",
                    "Interesting! Tell me more about it.",
                    "I appreciate your thoughts on this.",
                    "That's a good point. What do you think?"
                }
            };

            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private string GetDefaultResponse(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => "I'm here to help you practice English. Please continue the conversation!",
                "JP" => "日本語の練習をお手伝いします。会話を続けてください！",
                "ZH" => "我在这里帮助你练习中文。请继续对话！",
                _ => "Let's continue our conversation practice!"
            };
        }

        private async Task<ConversationEvaluationDto> GenerateEvaluationAsync(ConversationSession session)
        {
            try
            {
                var messageCount = session.ConversationMessages?.Count(m => m.Sender == MessageSender.User) ?? 0;
                var duration = (int)(DateTime.UtcNow - session.StartedAt).TotalSeconds;

                // Sử dụng AI để đánh giá nếu có
                if (_geminiService != null && messageCount > 0)
                {
                    var conversationHistory = session.ConversationMessages?
                        .OrderBy(m => m.SequenceOrder)
                        .Select(m => $"{m.Sender}: {m.MessageContent}")
                        .ToList() ?? new List<string>();

                    var evaluationPrompt = $@"
Evaluate this language learning conversation in {session.Language?.LanguageName}:

Conversation level: {session.DifficultyLevel}
Topic: {session.Topic?.Name}
Duration: {duration} seconds
Messages from user: {messageCount}

Conversation history:
{string.Join("\n", conversationHistory)}

Please provide scores (0-100) for:
1. Overall fluency
2. Grammar accuracy  
3. Vocabulary usage
4. Cultural appropriateness

Also provide:
- Key strengths (2-3 points)
- Areas for improvement (2-3 points)
- Specific feedback for motivation

Format as JSON with clear numeric scores.";

                    try
                    {
                        var aiEvaluation = await _geminiService.EvaluateConversationAsync(evaluationPrompt);
                        if (aiEvaluation != null)
                        {
                            return new ConversationEvaluationDto
                            {
                                SessionId = session.ConversationSessionID,
                                OverallScore = aiEvaluation.OverallScore,
                                FluentScore = aiEvaluation.FluentScore,
                                GrammarScore = aiEvaluation.GrammarScore,
                                VocabularyScore = aiEvaluation.VocabularyScore,
                                CulturalScore = aiEvaluation.CulturalScore,
                                AIFeedback = aiEvaluation.AIFeedback,
                                Improvements = aiEvaluation.Improvements,
                                Strengths = aiEvaluation.Strengths,
                                TotalMessages = messageCount,
                                SessionDuration = duration
                            };
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "AI evaluation failed, using simple evaluation");
                    }
                }

                // Fallback: đánh giá đơn giản
                return GenerateSimpleEvaluation(session, messageCount, duration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating evaluation");
                return GenerateSimpleEvaluation(session, 0, 0);
            }
        }

        private ConversationEvaluationDto GenerateSimpleEvaluation(ConversationSession session, int messageCount, int duration)
        {
            // Điểm số đơn giản dựa trên số tin nhắn và thời gian
            var baseScore = Math.Min(100, 50 + (messageCount * 5));
            var timeBonus = duration > 300 ? 10 : 0; // Bonus nếu trò chuyện > 5 phút
            var overall = Math.Min(100, baseScore + timeBonus);

            return new ConversationEvaluationDto
            {
                SessionId = session.ConversationSessionID,
                OverallScore = overall,
                FluentScore = overall * 0.9f,
                GrammarScore = overall * 0.85f,
                VocabularyScore = overall * 0.95f,
                CulturalScore = overall * 0.8f,
                AIFeedback = GenerateFeedbackMessage(messageCount, duration, session.Language?.LanguageName ?? ""),
                Improvements = GenerateImprovementSuggestions(messageCount, session.DifficultyLevel),
                Strengths = GenerateStrengthPoints(messageCount, duration),
                TotalMessages = messageCount,
                SessionDuration = duration
            };
        }

        private string GenerateFeedbackMessage(int messageCount, int duration, string languageName)
        {
            if (messageCount == 0)
                return $"Chúc mừng bạn đã bắt đầu thử thách conversation partners với {languageName}! Lần tới hãy thử gửi thêm tin nhắn để có trải nghiệm tốt hơn.";

            if (messageCount < 3)
                return $"Tốt! Bạn đã bắt đầu cuộc trò chuyện bằng {languageName}. Hãy thử nói nhiều hơn để cải thiện kỹ năng giao tiếp.";

            if (messageCount < 10)
                return $"Rất tốt! Bạn đã duy trì được cuộc trò chuyện bằng {languageName}. Kỹ năng giao tiếp của bạn đang được cải thiện.";

            return $"Xuất sắc! Bạn đã có một cuộc trò chuyện rất tích cực bằng {languageName}. Hãy tiếp tục thực hành để trở nên thành thạo hơn!";
        }

        private string GenerateImprovementSuggestions(int messageCount, string difficultyLevel)
        {
            var suggestions = new List<string>();

            if (messageCount < 5)
                suggestions.Add("Hãy thử gửi thêm tin nhắn để có cuộc trò chuyện dài hơn");

            suggestions.Add("Thử sử dụng nhiều từ vựng đa dạng hơn");
            suggestions.Add("Hãy đặt thêm câu hỏi để duy trì cuộc trò chuyện");
            suggestions.Add("Thử diễn đạt ý kiến của bạn một cách chi tiết hơn");

            if (difficultyLevel.Contains("A1") || difficultyLevel.Contains("N5") || difficultyLevel.Contains("HSK 1"))
                suggestions.Add("Luyện tập phát âm cơ bản để giao tiếp tự nhiên hơn");
            else
                suggestions.Add("Thử sử dụng các cấu trúc câu phức tạp hơn");

            return string.Join(". ", suggestions.Take(3));
        }

        private string GenerateStrengthPoints(int messageCount, int duration)
        {
            var strengths = new List<string>();

            if (messageCount >= 10)
                strengths.Add("Bạn rất tích cực tham gia và duy trì cuộc trò chuyện tốt");
            else if (messageCount >= 5)
                strengths.Add("Bạn có thể trả lời các câu hỏi một cách hợp lý");
            else
                strengths.Add("Bạn đã bắt đầu giao tiếp và có thái độ học tập tích cực");

            if (duration > 600) // 10 phút
                strengths.Add("Bạn có khả năng duy trì cuộc trò chuyện trong thời gian dài");
            else if (duration > 300) // 5 phút
                strengths.Add("Bạn kiên trì trong việc thực hành giao tiếp");

            strengths.Add("Bạn sẵn sàng thử thách bản thân với công nghệ học ngôn ngữ mới");

            return string.Join(". ", strengths.Take(3));
        }

        private string GetDefaultRole(string topicName)
        {
            return topicName.ToLower() switch
            {
                var topic when topic.Contains("restaurant") || topic.Contains("ẩm thực") => "Restaurant Staff",
                var topic when topic.Contains("travel") || topic.Contains("du lịch") => "Travel Guide",
                var topic when topic.Contains("shopping") || topic.Contains("mua sắm") => "Shop Assistant",
                var topic when topic.Contains("work") || topic.Contains("công việc") => "Colleague",
                var topic when topic.Contains("school") || topic.Contains("học") => "Study Partner",
                var topic when topic.Contains("health") || topic.Contains("sức khỏe") => "Health Advisor",
                var topic when topic.Contains("family") || topic.Contains("gia đình") => "Friend",
                _ => "Conversation Partner"
            };
        }

        private string GetDefaultFirstMessage(string language, string topic, string level)
        {
            var isBasicLevel = level.Contains("A1") || level.Contains("N5") || level.Contains("HSK 1");

            if (language.Contains("English") || language.Contains("Anh"))
            {
                return isBasicLevel
                    ? $"Hello! Let's practice talking about {topic}. How are you today?"
                    : $"Hi there! I'm excited to discuss {topic} with you. What would you like to start with?";
            }
            else if (language.Contains("Japanese") || language.Contains("Nhật"))
            {
                return isBasicLevel
                    ? $"こんにちは！{topic}について話しましょう。今日はどうですか？"
                    : $"こんにちは！{topic}について話し合うのが楽しみです。何から始めましょうか？";
            }
            else if (language.Contains("Chinese") || language.Contains("Trung"))
            {
                return isBasicLevel
                    ? $"你好！我们来聊聊{topic}吧。你今天怎么样？"
                    : $"你好！我很期待和你讨论{topic}。你想从什么开始？";
            }

            return "Hello! Let's start our conversation practice. What would you like to talk about?";
        }

        private ConversationSessionDto MapToConversationSessionDto(ConversationSession session, List<ConversationMessage> messages)
        {
            return new ConversationSessionDto
            {
                SessionId = session.ConversationSessionID,
                SessionName = session.SessionName,
                LanguageName = session.Language?.LanguageName ?? "",
                TopicName = session.Topic?.Name ?? "",
                DifficultyLevel = session.DifficultyLevel,
                CharacterRole = session.AICharacterRole ?? "",
                ScenarioDescription = session.GeneratedScenario ?? "",
                Messages = messages.Select(MapToMessageDto).ToList(),
                Status = session.Status,
                StartedAt = session.StartedAt,
                OverallScore = session.OverallScore,
                AIFeedback = session.AIFeedback
            };
        }

        private ConversationMessageDto MapToMessageDto(ConversationMessage message)
        {
            return new ConversationMessageDto
            {
                MessageId = message.ConversationMessageID,
                Sender = message.Sender,
                MessageContent = message.MessageContent,
                MessageType = message.MessageType,
                AudioUrl = message.AudioUrl,
                SequenceOrder = message.SequenceOrder,
                SentAt = message.SentAt
            };
        }

        #endregion
    }
}
    
