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

        public async Task<ConversationSessionDto> StartConversationAsync(
     Guid userId,
     StartConversationRequestDto request)
        {
            Language language = null;
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (!await CanStartConversationAsync(user))
                {
                    throw new InvalidOperationException(
                        "You've reached your daily conversation limit. Please upgrade your plan.");
                }

                language = await _unitOfWork.Languages.GetByIdAsync(request.LanguageId);
                var topic = await _unitOfWork.Topics.GetByIdAsync(request.TopicId);

                if (language == null || topic == null)
                    throw new ArgumentException("Language or topic not found");

                var activeGlobalPrompt = await _unitOfWork.GlobalConversationPrompts.GetActiveDefaultPromptAsync();
                if (activeGlobalPrompt == null)
                    throw new InvalidOperationException("No active prompt configured");

                var conversationContext = new ConversationContextDto
                {
                    Language = language.LanguageName,
                    LanguageCode = language.LanguageCode,
                    Topic = topic.Name,
                    TopicDescription = topic.Description,
                    DifficultyLevel = request.DifficultyLevel,
                    MasterPrompt = $@"{activeGlobalPrompt.MasterPromptTemplate}

CRITICAL INSTRUCTION: You MUST respond ONLY in {language.LanguageName}. 
Never respond in Vietnamese or any other language, regardless of what language the user writes in.",
                    ScenarioGuidelines = activeGlobalPrompt.ScenarioGuidelines ?? "",
                    RoleplayInstructions = activeGlobalPrompt.RoleplayInstructions ?? "",
                    EvaluationCriteria = activeGlobalPrompt.EvaluationCriteria ?? ""
                };


                var generatedContent = await _geminiService.GenerateConversationContentAsync(conversationContext);
                var characterRole = ResolveCharacterRole(topic.Name, generatedContent.AIRole);

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
                    AICharacterRole = characterRole,
                    GeneratedSystemPrompt = generatedContent.SystemPrompt,
                    StartedAt = DateTime.UtcNow,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationSessions.CreateAsync(session);


                var tasks = NormalizeAndSelectTasks(topic.Name, generatedContent.Tasks ?? new List<ConversationTaskDto>());

                int sequence = 1;

                foreach (var taskDto in tasks)
                {
                    var conversationTask = new ConversationTask
                    {
                        TaskID = Guid.NewGuid(),
                        ConversationSessionID = session.ConversationSessionID,
                        TaskDescription = taskDto.TaskDescription,
                        TaskContext = taskDto.TaskContext,
                        TaskSequence = sequence++,
                        Status = "Pending",
                        IsCompleted = false,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await _unitOfWork.ConversationTasks.CreateAsync(conversationTask);
                    session.Tasks.Add(conversationTask);
                }

                var firstMessageText = EnsureTargetLanguageOnly(
                    generatedContent.FirstMessage ?? GetDefaultFirstMessage(language.LanguageName, topic.Name, request.DifficultyLevel, language.LanguageCode)
                );

                var firstMessage = new ConversationMessage
                {
                    ConversationMessageID = Guid.NewGuid(),
                    ConversationSessionID = session.ConversationSessionID,
                    Sender = MessageSender.AI,
                    MessageContent = firstMessageText,
                    MessageType = MessageType.Text,
                    SequenceOrder = 1,
                    SentAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationMessages.CreateAsync(firstMessage);

                activeGlobalPrompt.UsageCount++;
                await _unitOfWork.GlobalConversationPrompts.UpdateAsync(activeGlobalPrompt);

                user.ConversationsUsedToday++;
                await _unitOfWork.Users.UpdateAsync(user);

                await _hubContext.Clients.Group($"User_{userId}")
     .SendAsync("ConversationStarted", new
     {
         sessionId = session.ConversationSessionID,
         sessionName = session.SessionName,
         languageName = language.LanguageName,
         topicName = topic.Name,
         scenario = generatedContent.ScenarioDescription,

         tasks = session.Tasks
             .GroupBy(t => t.TaskID) // Group by TaskID to remove duplicates
             .Select(g => g.First()) // Take first of each group
             .OrderBy(t => t.TaskSequence)
             .Select(t => new
             {
                 t.TaskID,
                 t.TaskDescription,
                 t.TaskSequence,
                 t.Status
             }),
         startedAt = DateTime.UtcNow
     });

                return MapToConversationSessionDto(session, new List<ConversationMessage> { firstMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                throw;
            }
        }
        private List<ConversationTaskDto> CreateDefaultTasksForTopic(string topicName)
        {
           
            return topicName.ToLower() switch
            {
                var t when t.Contains("interview") || t.Contains("phỏng vấn") || t.Contains("面接") || t.Contains("採用") => new List<ConversationTaskDto>
        {
            new() { TaskDescription = "Self-introduction and strengths", TaskSequence = 1 },
            new() { TaskDescription = "Why our company", TaskSequence = 2 },
            new() { TaskDescription = "Biggest technical challenge", TaskSequence = 3 }
        },
                var t when t.Contains("lost luggage") || t.Contains("lost baggage") || t.Contains("baggage claim") || t.Contains("mất hành lý") || t.Contains("hành lý thất lạc") => new List<ConversationTaskDto>
        {
            new() { TaskDescription = "Report your missing bag and describe it.", TaskSequence = 1 },
            new() { TaskDescription = "Ask how and when it will be delivered.", TaskSequence = 2 },
            new() { TaskDescription = "Provide contact details clearly.", TaskSequence = 3 }
        },
                var t when t.Contains("restaurant") || t.Contains("ẩm thực") => new List<ConversationTaskDto>
        {
            new() { TaskDescription = "Ask the waiter for a recommendation", TaskSequence = 1 },
            new() { TaskDescription = "Order your main course", TaskSequence = 2 },
            new() { TaskDescription = "Ask for the bill", TaskSequence = 3 }
        },
                _ => new List<ConversationTaskDto>
        {
            new() { TaskDescription = "Start the conversation", TaskSequence = 1 },
            new() { TaskDescription = "Ask follow-up questions", TaskSequence = 2 },
            new() { TaskDescription = "Express your thoughts", TaskSequence = 3 }
        }
            };
        }
        private async Task<bool> CanStartConversationAsync(User user)
        {
            if (user == null) return false;

            // Check if user has active subscription (not Free tier)
            var activeSubscription = user.Subscriptions?
                .FirstOrDefault(s => s.IsActive && s.StartDate <= DateTime.UtcNow &&
                    (s.EndDate == null || s.EndDate > DateTime.UtcNow));

            // If subscription exists and user still has quota
            if (activeSubscription != null)
            {
                var quota = activeSubscription.ConversationQuota;

                // Reset daily count if needed
                if (user.LastConversationResetDate.Date < DateTime.UtcNow.Date)
                {
                    user.ConversationsUsedToday = 0;
                    user.LastConversationResetDate = DateTime.UtcNow;
                }

                return user.ConversationsUsedToday < quota;
            }

            // Default free tier (2 conversations)
            if (user.LastConversationResetDate.Date < DateTime.UtcNow.Date)
            {
                user.ConversationsUsedToday = 0;
                user.LastConversationResetDate = DateTime.UtcNow;
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }

            return user.ConversationsUsedToday < user.DailyConversationLimit;
        }

        // Complete a task
        public async Task<ConversationTaskDto> CompleteTaskAsync(Guid userId, CompleteTaskRequestDto request)
        {
            try
            {
                var session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(request.SessionId);

                if (session == null || session.UserId != userId)
                    throw new ArgumentException("Session not found or access denied");

                var task = session.Tasks.FirstOrDefault(t => t.TaskID == request.TaskId);
                if (task == null)
                    throw new ArgumentException("Task not found");

                task.Status = "Completed";
                task.IsCompleted = true;
                task.CompletionNotes = request.CompletionNotes;
                task.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.ConversationTasks.UpdateAsync(task);
                await _unitOfWork.SaveChangesAsync();

                await _hubContext.Clients.Group($"Conversation_{request.SessionId}")
                    .SendAsync("TaskCompleted", new
                    {
                        taskId = task.TaskID,
                        taskDescription = task.TaskDescription,
                        completedAt = DateTime.UtcNow
                    });

                return MapToTaskDto(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing task");
                throw;
            }
        }

        public async Task<ConversationMessageDto> SendMessageAsync(Guid userId, SendMessageRequestDto request)
        {
            ConversationSession session = null;
            try
            {
                session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(request.SessionId);

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

                // Generate AI response
                var aiResponse = await GenerateAIResponseAsync(session, request.MessageContent);

                // Add translation hint if user message is in Vietnamese
                var enhancedAIResponse = EnhanceResponseWithTranslationHint(
                    aiResponse,
                    request.MessageContent,
                    session.Language?.LanguageCode ?? "EN"
                );

                var finalAIResponse = EnsureTargetLanguageOnly(enhancedAIResponse);

                var aiMessage = new ConversationMessage
                {
                    ConversationMessageID = Guid.NewGuid(),
                    ConversationSessionID = request.SessionId,
                    Sender = MessageSender.AI,
                    MessageContent = finalAIResponse,
                    MessageType = MessageType.Text,
                    SequenceOrder = nextSequence + 1,
                    SentAt = DateTime.UtcNow
                };

                await _unitOfWork.ConversationMessages.CreateAsync(aiMessage);

                session.MessageCount += 2;
                session.UpdatedAt = DateTime.UtcNow;
                await _unitOfWork.ConversationSessions.UpdateAsync(session);

                await _unitOfWork.SaveChangesAsync();

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

                var languageCode = session?.Language?.LanguageCode ?? "EN";
                var errorMessage = languageCode.ToUpper() switch
                {
                    "EN" => "Cannot send message",
                    "JP" => "メッセージを送信できません",
                    "ZH" => "无法发送消息",
                    _ => "Cannot send message"
                };

                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("MessageError", new
                    {
                        sessionId = request.SessionId,
                        error = errorMessage,
                        details = ex.Message
                    });

                throw;
            }
        }

        private string EnhanceResponseWithTranslationHint(string aiResponse, string userMessage, string targetLanguageCode)
        {
            // Check if user message is in Vietnamese
            if (!IsVietnamese(userMessage))
                return aiResponse;

            // Only add hint if learning a language other than Vietnamese
            if (targetLanguageCode.ToUpper() == "VI")
                return aiResponse;

            var translationHint = GetTranslationHint(userMessage, targetLanguageCode);

            var hintLabel = targetLanguageCode.ToUpper() switch
            {
                "EN" => "Hint",
                "JP" => "ヒント",
                "ZH" => "提示",
                _ => "Hint"
            };

            return $"{aiResponse}\n\n{hintLabel}: {translationHint}";
        }

        private bool IsVietnamese(string text)
        {
            // Check for common Vietnamese diacritical marks
            var vietnameseDiacritics = new[] { 'ả', 'ă', 'â', 'ấ', 'ầ', 'ẩ', 'ẫ', 'ậ',
                                       'đ', 'ế', 'ề', 'ễ', 'ệ', 'ì', 'í', 'ỉ',
                                       'ĩ', 'ị', 'ố', 'ồ', 'ổ', 'ỗ', 'ộ', 'ớ',
                                       'ờ', 'ở', 'ỡ', 'ợ', 'ù', 'ú', 'ủ', 'ũ',
                                       'ụ', 'ứ', 'ừ', 'ử', 'ữ', 'ự', 'ỳ', 'ý',
                                       'ỷ', 'ỹ', 'ỵ' };

            return text.ToLower().Any(c => vietnameseDiacritics.Contains(c));
        }

        private string GetTranslationHint(string vietnameseText, string targetLanguageCode)
        {
            // Use Gemini to translate Vietnamese to target language with "Hint:" prefix
            if (_geminiService != null)
            {
                try
                {
                    var languageName = targetLanguageCode.ToUpper() switch
                    {
                        "EN" => "English",
                        "JP" => "Japanese",
                        "ZH" => "Chinese",
                        _ => "English"
                    };

                    var translationTask = _geminiService.TranslateTextAsync(
                        vietnameseText,
                        "Vietnamese",
                        languageName
                    );

                    translationTask.Wait(5000); // Wait max 5 seconds
                    return translationTask.IsCompletedSuccessfully
                        ? (translationTask.Result ?? vietnameseText)
                        : GetSimpleTranslation(vietnameseText, targetLanguageCode);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error translating Vietnamese text");
                    return GetSimpleTranslation(vietnameseText, targetLanguageCode);
                }
            }

            return GetSimpleTranslation(vietnameseText, targetLanguageCode);
        }

        private string GetSimpleTranslation(string vietnameseText, string targetLanguageCode)
        {
            // Fallback: simple common phrase translations
            var translations = new Dictionary<string, Dictionary<string, string>>
    {
        { "EN", new Dictionary<string, string>
        {
            { "xin chào", "Hello" },
            { "tạm biệt", "Goodbye" },
            { "cảm ơn", "Thank you" },
            { "không", "No" },
            { "có", "Yes" },
            { "tôi tên là", "My name is" },
            { "bạn khỏe không", "How are you" },
            { "rất vui gặp bạn", "Nice to meet you" }
        }},
        { "JP", new Dictionary<string, string>
        {
            { "xin chào", "こんにちは" },
            { "tạm biệt", "さようなら" },
            { "cảm ơn", "ありがとう" },
            { "không", "いいえ" },
            { "có", "はい" },
            { "tôi tên là", "私の名前は" },
            { "bạn khỏe không", "元気ですか" },
            { "rất vui gặp bạn", "お会いして嬉しいです" }
        }},
        { "ZH", new Dictionary<string, string>
        {
            { "xin chào", "你好" },
            { "tạm biệt", "再见" },
            { "cảm ơn", "谢谢" },
            { "không", "不" },
            { "có", "是" },
            { "tôi tên là", "我叫" },
            { "bạn khỏe không", "你好吗" },
            { "rất vui gặp bạn", "很高兴认识你" }
        }}
    };

            var key = targetLanguageCode.ToUpper();
            if (!translations.ContainsKey(key))
                return vietnameseText;

            var lowerText = vietnameseText.ToLower();
            foreach (var phrase in translations[key])
            {
                if (lowerText.Contains(phrase.Key))
                    return phrase.Value;
            }

            return vietnameseText;
        }
        public async Task<ConversationEvaluationDto> EndConversationAsync(Guid userId, Guid sessionId)
        {
            ConversationSession session = null;
            try
            {
                session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(sessionId);

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

                var languageCode = session?.Language?.LanguageCode ?? "EN";
                var errorMessage = languageCode.ToUpper() switch
                {
                    "EN" => "Cannot evaluate conversation",
                    "JP" => "会話を評価できません",
                    "ZH" => "无法评估对话",
                    _ => "Cannot evaluate conversation"
                };

                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("EvaluationError", new
                    {
                        sessionId,
                        error = errorMessage
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
                    Tasks = session.Tasks
                        .OrderBy(t => t.TaskSequence)
                        .Select(MapToTaskDto)
                        .ToList(),
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
                if (_geminiService != null)
                {
                    return await _geminiService.GenerateConversationContentAsync(context);
                }

                return new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice {context.Topic} in {context.Language} at {context.DifficultyLevel} level",
                    AIRole = GetDefaultRole(context.Topic),
                    SystemPrompt = context.MasterPrompt.Replace("{LANGUAGE}", context.Language)
                        .Replace("{TOPIC}", context.Topic)
                        .Replace("{DIFFICULTY_LEVEL}", context.DifficultyLevel),
                   
                    FirstMessage = GetDefaultFirstMessage(context.Language, context.Topic, context.DifficultyLevel, context.LanguageCode)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating conversation content");

                return new GeneratedConversationContentDto
                {
                    ScenarioDescription = $"Practice conversation about {context.Topic}",
                    AIRole = "Conversation Partner",
                    SystemPrompt = $"You are helping someone practice {context.Language}",
                    FirstMessage = "Hello! Let's start our conversation practice."
                };
            }
        
        }
        private List<ConversationTaskDto> NormalizeAndSelectTasks(string topicName, List<ConversationTaskDto> aiTasks)
        {
            var tasks = aiTasks ?? new List<ConversationTaskDto>();

            // Scenario-specific override or fallback when none
            bool isLostLuggageTopic = !string.IsNullOrWhiteSpace(topicName) &&
                (topicName.ToLower().Contains("lost luggage") ||
                 topicName.ToLower().Contains("lost baggage") ||
                 topicName.ToLower().Contains("baggage claim") ||
                 topicName.ToLower().Contains("mất hành lý") ||
                 topicName.ToLower().Contains("hành lý thất lạc"));

            bool isInterviewTopic = !string.IsNullOrWhiteSpace(topicName) &&
                (topicName.ToLower().Contains("interview") ||
                 topicName.ToLower().Contains("phỏng vấn") ||
                 topicName.ToLower().Contains("面接") ||
                 topicName.ToLower().Contains("採用"));

            if (isLostLuggageTopic || isInterviewTopic || tasks.Count == 0)
            {
                tasks = CreateDefaultTasksForTopic(topicName);
            }

            // Enhanced deduplication logic
            var unique = new HashSet<string>();
            var result = new List<ConversationTaskDto>(capacity: 3);

            foreach (var t in tasks.OrderBy(t => t.TaskSequence))
            {
                if (string.IsNullOrWhiteSpace(t.TaskDescription)) continue;

                var shortened = ShortenTask(t.TaskDescription);

                // Improved key generation for multilingual content
                var normalizedDescription = NormalizeForDedup(shortened);
                var normalizedContext = NormalizeForDedup(t.TaskContext ?? "");
                var key = $"{normalizedDescription}#{normalizedContext}";

                if (string.IsNullOrWhiteSpace(normalizedDescription)) continue;
                if (!unique.Add(key))
                {
                    _logger.LogDebug("Skipping duplicate task: {TaskDescription}", shortened);
                    continue;
                }

                result.Add(new ConversationTaskDto
                {
                    TaskDescription = shortened,
                    TaskContext = t.TaskContext,
                    TaskSequence = result.Count + 1
                });

                if (result.Count >= 3) break;
            }

         
            if (result.Count < 3)
            {
                foreach (var dt in CreateDefaultTasksForTopic(topicName))
                {
                    var shortened = ShortenTask(dt.TaskDescription);
                    var normalizedDescription = NormalizeForDedup(shortened);
                    var normalizedContext = NormalizeForDedup(dt.TaskContext ?? "");
                    var key = $"{normalizedDescription}#{normalizedContext}";

                    if (!unique.Add(key)) continue;

                    result.Add(new ConversationTaskDto
                    {
                        TaskDescription = shortened,
                        TaskContext = dt.TaskContext,
                        TaskSequence = result.Count + 1
                    });

                    if (result.Count >= 3) break;
                }
            }

            _logger.LogDebug("Generated {TaskCount} unique tasks for topic: {TopicName}", result.Count, topicName);
            return result;
        }


        private string ShortenTask(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return string.Empty;
            var s = description.Replace("\n", " ").Replace("\r", " ").Trim();

            // Remove common fillers
            foreach (var filler in new[] { "Please ", "Kindly ", "First, ", "Then, ", "Finally, ", "Next, " })
            {
                if (s.StartsWith(filler, StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(filler.Length).TrimStart();
                }
            }

            // Cut at first sentence boundary if very long
            var periodIndex = s.IndexOf('.');
            if (periodIndex > 0 && s.Length > 70)
            {
                s = s.Substring(0, periodIndex).Trim();
            }

            // If still long, keep first 8 words
            if (s.Length > 70)
            {
                var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                s = string.Join(' ', words.Take(8));
            }

            // Normalize punctuation
            s = s.Trim().TrimEnd('.', '!', '、', '。');

            // Capitalize first letter
            if (s.Length > 0)
            {
                s = char.ToUpperInvariant(s[0]) + (s.Length > 1 ? s.Substring(1) : string.Empty);
            }

            return s;
        }

        private string NormalizeForDedup(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return string.Empty;

        
            var pipeIndex = description.IndexOf('|');
            var textToNormalize = pipeIndex > 0 ? description.Substring(0, pipeIndex) : description;

            var lower = textToNormalize.ToLowerInvariant().Trim();
            var builder = new StringBuilder(lower.Length);

            foreach (var ch in lower)
            {
                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch))
                {
                    builder.Append(ch);
                }
            }

            var normalized = string.Join(' ', builder.ToString()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries));

            return normalized;
        }

        private async Task<string> GenerateAIResponseAsync(ConversationSession session, string userMessage)
        {
            try
            {
                if (_geminiService != null)
                {
                  
                    var enforcedSystemPrompt = $@"{session.GeneratedSystemPrompt}

IMPORTANT: You MUST respond ONLY in {session.Language?.LanguageName ?? "English"}.
Do NOT respond in any other language (including Vietnamese), regardless of the user's input language.
Always respond in {session.Language?.LanguageName ?? "English"} only.";

                    var context = new
                    {
                        SystemPrompt = enforcedSystemPrompt,
                        UserMessage = userMessage,
                        ConversationHistory = session.ConversationMessages?
                            .OrderBy(m => m.SequenceOrder)
                            .Select(m => $"{m.Sender}: {m.MessageContent}")
                            .ToList() ?? new List<string>()
                    };

                    var response = await _geminiService.GenerateResponseAsync(
                        context.SystemPrompt,
                        userMessage,
                        context.ConversationHistory
                    );

                    return response;
                }

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
            var baseScore = Math.Min(100, 50 + (messageCount * 5));
            var timeBonus = duration > 300 ? 10 : 0;
            var overall = Math.Min(100, baseScore + timeBonus);

          
            var languageCode = session.Language?.LanguageCode ?? "EN";

            return new ConversationEvaluationDto
            {
                SessionId = session.ConversationSessionID,
                OverallScore = overall,
                FluentScore = overall * 0.9f,
                GrammarScore = overall * 0.85f,
                VocabularyScore = overall * 0.95f,
                CulturalScore = overall * 0.8f,
             
                AIFeedback = GenerateFeedbackMessage(messageCount, duration, session.Language?.LanguageName ?? "", languageCode),
                Improvements = GenerateImprovementSuggestions(messageCount, session.DifficultyLevel, languageCode),
                Strengths = GenerateStrengthPoints(messageCount, duration, languageCode),
                TotalMessages = messageCount,
                SessionDuration = duration
            };
        }

        private string GenerateFeedbackMessage(int messageCount, int duration, string languageName, string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => messageCount == 0
                    ? "Great start! Try sending more messages to get better practice experience."
                    : messageCount < 3
                    ? "Good! You've started the conversation in English. Try speaking more to improve."
                    : messageCount < 10
                    ? "Very good! You're maintaining the conversation. Your communication is improving."
                    : "Excellent! You've had a very active conversation in English. Keep practicing!",

                "JP" => messageCount == 0
                    ? "素晴らしい！もっとたくさんメッセージを送ってみてください。"
                    : messageCount < 3
                    ? "いいですね！英語で会話を始めました。もっと話して改善しましょう。"
                    : messageCount < 10
                    ? "とても良いです！会話を続けています。コミュニケーションが改善されています。"
                    : "素晴らしい！英語で非常に活動的な会話ができました。練習を続けてください！",

                "ZH" => messageCount == 0
                    ? "很好的开始！尝试发送更多消息以获得更好的练习体验。"
                    : messageCount < 3
                    ? "很好！你已经开始用中文交谈。多说话来改进。"
                    : messageCount < 10
                    ? "非常好！你在维持对话。你的交流能力在改进。"
                    : "优秀！你进行了一次非常活跃的中文对话。继续练习！",

                _ => "Good effort! Keep practicing!"
            };
        }

        private string GenerateImprovementSuggestions(int messageCount, string difficultyLevel, string languageCode)
        {
            var suggestions = languageCode.ToUpper() switch
            {
                "EN" => new[]
                {
            messageCount < 5 ? "Try sending longer messages" : "Use more diverse vocabulary",
            "Ask more questions to keep the conversation going",
            "Express your ideas in more detail",
            difficultyLevel.Contains("A1") || difficultyLevel.Contains("N5")
                ? "Practice basic pronunciation"
                : "Try using more complex sentence structures"
        },

                "JP" => new[]
                {
            messageCount < 5 ? "もっと長いメッセージを送ってみてください" : "より多くの多様な語彙を使用してください",
            "より多くの質問をして会話を続けてください",
            "あなたの考えをより詳しく表現してください",
            difficultyLevel.Contains("A1") || difficultyLevel.Contains("N5")
                ? "基本的な発音を練習してください"
                : "より複雑な文構造を使ってみてください"
        },

                "ZH" => new[]
                {
            messageCount < 5 ? "尝试发送更长的消息" : "使用更多样化的词汇",
            "提出更多问题来继续对话",
            "更详细地表达你的想法",
            difficultyLevel.Contains("A1") || difficultyLevel.Contains("N5")
                ? "练习基本发音"
                : "尝试使用更复杂的句子结构"
        },

                _ => new[] { "Keep practicing!", "Try speaking more", "Expand your vocabulary" }
            };

            return string.Join(". ", suggestions.Take(3)) + ".";
        }
        private string GenerateStrengthPoints(int messageCount, int duration, string languageCode)
        {
            var strengths = languageCode.ToUpper() switch
            {
                "EN" => new[]
                {
            messageCount >= 10
                ? "You actively participated and maintained good conversation flow"
                : messageCount >= 5
                ? "You answered questions reasonably well"
                : "You started communicating with a positive learning attitude",

            duration > 600
                ? "You can maintain conversation for extended periods"
                : duration > 300
                ? "You showed perseverance in your practice"
                : "You took the initiative to challenge yourself",

            "You're willing to embrace new language learning technology"
        },

                "JP" => new[]
                {
            messageCount >= 10
                ? "積極的に参加して、良い会話の流れを保ちました"
                : messageCount >= 5
                ? "質問に理にかなった答えをしました"
                : "積極的な学習態度で通信を開始しました",

            duration > 600
                ? "長時間会話を維持できます"
                : duration > 300
                ? "練習に忍耐力を示しました"
                : "進んで自分自身に挑戦しました",

            "新しい言語学習技術を受け入れることをいとわない"
        },

                "ZH" => new[]
                {
            messageCount >= 10
                ? "你积极参与并保持了良好的对话流程"
                : messageCount >= 5
                ? "你很好地回答了问题"
                : "你以积极的学习态度开始交流",

            duration > 600
                ? "你可以长时间维持对话"
                : duration > 300
                ? "你在练习中表现出了毅力"
                : "你主动挑战自己",

            "你愿意接受新的语言学习技术"
        },

                _ => new[] { "Great effort!", "Good practice", "Positive attitude" }
            };

            return string.Join(". ", strengths.Take(3)) + ".";
        }

        private string GetDefaultFirstMessage(string language, string topic, string level, string languageCode)
        {
            var isBasicLevel = level.Contains("A1") || level.Contains("N5") || level.Contains("HSK 1");

            return languageCode.ToUpper() switch
            {
                "EN" => isBasicLevel
                    ? $"Hello! Let's practice talking about {topic}. How are you today?"
                    : $"Hi there! I'm excited to discuss {topic} with you. What would you like to start with?",

                "JP" => isBasicLevel
                    ? $"こんにちは！{topic}について話しましょう。今日はどうですか？"
                    : $"こんにちは！{topic}について話し合うのが楽しみです。何から始めましょうか？",

                "ZH" => isBasicLevel
                    ? $"你好！我们来聊聊{topic}吧。你今天怎么样？"
                    : $"你好！我很期待和你讨论{topic}。你想从什么开始？",

                _ => "Hello! Let's start our conversation practice."
            };
        }
        
        private string EnsureTargetLanguageOnly(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return text;
            var pipeIndex = text.IndexOf('|');
            return pipeIndex > 0 ? text.Substring(0, pipeIndex).Trim() : text.Trim();
        }
        private string GetDefaultRole(string topicName)
        {
            return topicName.ToLower() switch
            {
                var topic when topic.Contains("interview") || topic.Contains("phỏng vấn") || topic.Contains("面接") || topic.Contains("採用") => "採用担当者（面接官） | Người quản lý tuyển dụng (Người phỏng vấn)",
                var topic when topic.Contains("lost luggage") || topic.Contains("lost baggage") || topic.Contains("baggage claim") || topic.Contains("mất hành lý") || topic.Contains("hành lý thất lạc") => "Baggage Service Agent",
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

        private string ResolveCharacterRole(string topicName, string aiRoleCandidate)
        {
            var topic = topicName?.ToLower() ?? string.Empty;
            if (topic.Contains("interview") || topic.Contains("phỏng vấn") || topic.Contains("面接") || topic.Contains("採用"))
            {
                return "採用担当者（面接官） | Người quản lý tuyển dụng (Người phỏng vấn)";
            }

            if (!string.IsNullOrWhiteSpace(aiRoleCandidate))
            {
                return aiRoleCandidate.Trim();
            }

            return GetDefaultRole(topicName ?? string.Empty);
        }
    

        public async Task<ConversationUsageDto> GetConversationUsageAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null)
                    throw new ArgumentException("User not found");

                var activeSubscription = user.Subscriptions?
                    .FirstOrDefault(s => s.IsActive && s.StartDate <= DateTime.UtcNow &&
                        (s.EndDate == null || s.EndDate > DateTime.UtcNow));

                var dailyLimit = activeSubscription?.ConversationQuota ?? user.DailyConversationLimit;
                var subscriptionType = activeSubscription?.SubscriptionType ?? "Free";

                return new ConversationUsageDto
                {
                    ConversationsUsedToday = user.ConversationsUsedToday,
                    DailyLimit = dailyLimit,
                    SubscriptionType = subscriptionType,
                    ResetDate = user.LastConversationResetDate.Date.AddDays(1)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation usage for user {UserId}", userId);
                throw;
            }
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
                Tasks = session.Tasks
                    .GroupBy(t => t.TaskID) // Group by TaskID to eliminate duplicates
                    .Select(g => g.First()) // Take first occurrence of each group
                    .OrderBy(t => t.TaskSequence)
                    .Select(MapToTaskDto)
                    .ToList(),
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
        private ConversationTaskDto MapToTaskDto(ConversationTask task)
        {
            return new ConversationTaskDto
            {
                TaskId = task.TaskID,
                TaskDescription = task.TaskDescription,
                TaskContext = task.TaskContext,
                TaskSequence = task.TaskSequence,
                Status = task.Status,
                IsCompleted = task.IsCompleted,
                CompletionNotes = task.CompletionNotes
            };
        }

        #endregion
    }
}
    
