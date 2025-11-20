using BLL.Hubs;
using BLL.IServices.AI;
using BLL.IServices.Coversation;
using BLL.IServices.Gamification;
using Common.Constants;
using Common.DTO.Conversation;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace BLL.Services
{
    public class ConversationPartnerService : IConversationPartnerService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IGeminiService _geminiService;
        private readonly ILogger<ConversationPartnerService> _logger;
        private readonly IHubContext<ConversationHub> _hubContext;
        private readonly IGamificationService _gamificationService;

        public ConversationPartnerService(
            IUnitOfWork unitOfWork,
            IGeminiService geminiService,
            ILogger<ConversationPartnerService> logger,
            IHubContext<ConversationHub> hubContext,
            IGamificationService gamificationService)
        {
            _unitOfWork = unitOfWork;
            _geminiService = geminiService;
            _logger = logger;
            _hubContext = hubContext;
            _gamificationService = gamificationService;
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
                    .OrderBy(ll => ll.OrderIndex)
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
            Language? language = null;
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (!await CanStartConversationAsync(user))
                {
                    throw new InvalidOperationException("You've reached your daily conversation limit. Please upgrade your plan.");
                }

                language = await _unitOfWork.Languages.GetByIdAsync(request.LanguageId);
                var topic = await _unitOfWork.Topics.GetByIdAsync(request.TopicId);

                if (language == null || topic == null)
                    throw new ArgumentException("Language or topic not found");

                // Ensure LearnerLanguage
                var learnerLanguages = await _unitOfWork.LearnerLanguages.GetAllAsync();
                var learnerLanguage = learnerLanguages.FirstOrDefault(ll => ll.UserId == userId && ll.LanguageId == request.LanguageId);
                if (learnerLanguage == null)
                {
                    learnerLanguage = new LearnerLanguage
                    {
                        LearnerLanguageId = Guid.NewGuid(),
                        UserId = userId,
                        LanguageId = request.LanguageId,
                        ProficiencyLevel = string.Empty
                    };
                    await _unitOfWork.LearnerLanguages.CreateAsync(learnerLanguage);
                    await _unitOfWork.SaveChangesAsync();
                }

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
                    ScenarioGuidelines = activeGlobalPrompt.ScenarioGuidelines ?? string.Empty,
                    RoleplayInstructions = activeGlobalPrompt.RoleplayInstructions ?? string.Empty,
                    EvaluationCriteria = activeGlobalPrompt.EvaluationCriteria ?? string.Empty
                };

                var generatedContent = await _geminiService.GenerateConversationContentAsync(conversationContext);

                // Make role concise and specific
                var rawRole = ResolveCharacterRole(topic.Name, generatedContent.AIRole);
                var characterRole = MakeConciseRole(rawRole, topic.Name);

                var session = new ConversationSession
                {
                    ConversationSessionID = Guid.NewGuid(),
                    LearnerId = learnerLanguage.LearnerLanguageId,
                    LanguageId = request.LanguageId,
                    TopicID = request.TopicId,
                    GlobalPromptID = activeGlobalPrompt.GlobalPromptID,
                    DifficultyLevel = request.DifficultyLevel,
                    SessionName = $"{topic.Name} - {request.DifficultyLevel}",
                    GeneratedScenario = generatedContent.ScenarioDescription,
                    AICharacterRole = characterRole,
                    GeneratedSystemPrompt = generatedContent.SystemPrompt,
                    StartedAt = TimeHelper.GetVietnamTime(),
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.ConversationSessions.CreateAsync(session);

                // Tasks
                var tasks = NormalizeAndSelectTasks(topic.Name, generatedContent.Tasks ?? new List<ConversationTaskDto>());
                var sequence = 1;
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
                        CreatedAt = TimeHelper.GetVietnamTime(),
                        UpdatedAt = TimeHelper.GetVietnamTime()
                    };
                    await _unitOfWork.ConversationTasks.CreateAsync(conversationTask);
                    session.Tasks.Add(conversationTask);
                }

                // First AI message
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
                    SentAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.ConversationMessages.CreateAsync(firstMessage);

                // Counters
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
 .GroupBy(t => t.TaskID)
 .Select(g => g.First())
 .OrderBy(t => t.TaskSequence)
 .Select(t => new
 {
     t.TaskID,
     t.TaskDescription,
     t.TaskSequence,
     t.Status
 }),
     startedAt = TimeHelper.GetVietnamTime()
 });

                return MapToConversationSessionDto(session, new List<ConversationMessage> { firstMessage });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                throw;
            }
        }

        public async Task<ConversationMessageDto> SendMessageAsync(Guid userId, SendMessageRequestDto request)
        {
            ConversationSession? session = null;
            try
            {
                session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(request.SessionId);
                if (session == null) throw new ArgumentException("Session not found or access denied");

                var ll = await _unitOfWork.LearnerLanguages.GetByIdAsync(session.LearnerId);
                if (ll == null || ll.UserId != userId) throw new ArgumentException("Session not found or access denied");

                if (session.Status != ConversationSessionStatus.Active) throw new InvalidOperationException("Session is not active");

                var nextSequence = (session.ConversationMessages?.Count ?? 0) + 1;

                // 1. Save User Message
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
                    SentAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.ConversationMessages.CreateAsync(userMessage);

           
                var grant = (!string.IsNullOrWhiteSpace(request.MessageContent) && request.MessageContent.Length >= 10) ? 2 : 1;
                await _gamificationService.AwardXpAsync(ll, grant, "Conversation message");

                var aiResponseDto = await GenerateAIResponseAsync(session, request.MessageContent);

                string finalAIResponseContent;

              
                if (aiResponseDto.IsOffTopic)
                {
                   
                    finalAIResponseContent = aiResponseDto.Content;
                }
                else
                {
                  
                    var enhancedAIResponse = EnhanceResponseWithTranslationHint(aiResponseDto.Content, request.MessageContent, session.Language?.LanguageCode ?? "EN");
                    finalAIResponseContent = EnsureTargetLanguageOnly(enhancedAIResponse);
                }

             
                var aiMessage = new ConversationMessage
                {
                    ConversationMessageID = Guid.NewGuid(),
                    ConversationSessionID = request.SessionId,
                    Sender = MessageSender.AI,
                    MessageContent = finalAIResponseContent,
                    MessageType = MessageType.Text,
                    SequenceOrder = nextSequence + 1,
                    SentAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.ConversationMessages.CreateAsync(aiMessage);
             

                session.MessageCount += 2;
                session.UpdatedAt = TimeHelper.GetVietnamTime();

            

                await _unitOfWork.ConversationSessions.UpdateAsync(session);
                await _unitOfWork.SaveChangesAsync();

              
                SynonymSuggestionDto? synonymSuggestions = null;
                try
                {
                   
                    var isVoicePlaceholder = request.MessageContent?.Equals("[Voice Message]", StringComparison.OrdinalIgnoreCase) == true;
                    var sessionLangName = session.Language?.LanguageName ?? string.Empty;
                    var userMsgLower = request.MessageContent?.ToLowerInvariant() ?? string.Empty;
                    bool languageMismatch = IsVietnamese(userMsgLower) && !sessionLangName.ToLowerInvariant().Contains("vi");

                    if (!string.IsNullOrWhiteSpace(request.MessageContent) &&
                        request.MessageContent.Length > 2 &&
                        !isVoicePlaceholder && !languageMismatch)
                    {
                        synonymSuggestions = await _geminiService.GenerateSynonymSuggestionsAsync(
                            request.MessageContent,
                            session.Language?.LanguageName ?? "English",
                            session.DifficultyLevel
                        );
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "EXCEPTION in synonym generation");
                }

                var aiMessageDto = MapToMessageDto(aiMessage);
                aiMessageDto.SynonymSuggestions = synonymSuggestions;

          
                await _hubContext.Clients.Group($"Conversation_{request.SessionId}")
                    .SendAsync("MessageProcessed", new
                    {
                        userMessage = MapToMessageDto(userMessage),
                        aiMessage = aiMessageDto,
                      
                        coachInfo = new
                        {
                            isOffTopic = aiResponseDto.IsOffTopic,
                            isTaskCompleted = aiResponseDto.IsTaskCompleted
                        }
                    });

                return aiMessageDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
             
                throw;
            }
        }
        public async Task<ConversationEvaluationDto> EndConversationAsync(Guid userId, Guid sessionId)
        {
            ConversationSession? session = null;
            try
            {
                // 1. Validate Session
                session = await _unitOfWork.ConversationSessions.GetSessionWithMessagesAsync(sessionId);
                if (session == null) throw new ArgumentException("Session not found or access denied");

                var ll = await _unitOfWork.LearnerLanguages.GetByIdAsync(session.LearnerId);
                if (ll == null || ll.UserId != userId) throw new ArgumentException("Session not found or access denied");

                if (session.Status != ConversationSessionStatus.Active) throw new InvalidOperationException("Session is not active");

                // 2. Chuẩn bị Transcript (Lịch sử chat) để gửi AI
                var messages = session.ConversationMessages.OrderBy(m => m.SequenceOrder).ToList();
                var transcript = new StringBuilder();
                foreach (var msg in messages)
                {
                    // Format: "User: Hello" / "AI: Hi there"
                    transcript.AppendLine($"{(msg.Sender == MessageSender.User ? "User" : "AI")}: {msg.MessageContent}");
                }

                // 3. Tạo Prompt đánh giá
                var prompt = $@"
Evaluate this roleplay conversation.
Roleplay Context: {session.AICharacterRole} (Topic: {session.Topic?.Name}).
Target Language: {session.Language?.LanguageName}.
Difficulty Level: {session.DifficultyLevel}.

TRANSCRIPT:
{transcript}
";

                // 4. GỌI AI (Quan trọng: Truyền LanguageName để feedback đúng tiếng đang học)
                var targetLanguage = session.Language?.LanguageName ?? "English";
                var evaluationResult = await _geminiService.EvaluateConversationAsync(prompt, targetLanguage);

                // 5. Cập nhật vào Database (Entity Session)
                session.Status = ConversationSessionStatus.Completed;
                session.EndedAt = TimeHelper.GetVietnamTime();
                session.Duration = (int)(TimeHelper.GetVietnamTime() - session.StartedAt).TotalSeconds;

                // Map điểm số
                session.OverallScore = evaluationResult.OverallScore;
                session.FluentScore = evaluationResult.FluentScore;
                session.GrammarScore = evaluationResult.GrammarScore;
                session.VocabularyScore = evaluationResult.VocabularyScore;
                session.CulturalScore = evaluationResult.CulturalScore;

                // Map Text Summary (Lưu vào DB các trường text chính)
                // Nếu DB không có chỗ lưu JSON chi tiết, ta lưu Summary vào AIFeedback
                session.AIFeedback = evaluationResult.ProgressSummary ?? evaluationResult.AIFeedback;
                session.Improvements = string.Join("\n", evaluationResult.AreasNeedingWork ?? new List<string>());
                session.Strengths = string.Join("\n", evaluationResult.PositivePatterns ?? new List<string>());

                await _unitOfWork.ConversationSessions.UpdateAsync(session);
                await _unitOfWork.SaveChangesAsync();

                // 6. Tính điểm XP (Gamification)
                var messagesFromUser = messages.Count(m => m.Sender == MessageSender.User);
                var baseXp = Math.Min(20, messagesFromUser * 2);
                baseXp += session.Duration >= 300 ? 10 : 0; // +10 XP nếu > 5 phút
                await _gamificationService.AwardXpAsync(ll!, baseXp, "Complete conversation session");

                // 7. Chuẩn bị DTO trả về (Chứa full dữ liệu chi tiết)
                var resultDto = new ConversationEvaluationDto
                {
                    SessionId = sessionId,
                    OverallScore = session.OverallScore ?? 0,
                    FluentScore = session.FluentScore ?? 0,
                    GrammarScore = session.GrammarScore ?? 0,
                    VocabularyScore = session.VocabularyScore ?? 0,
                    CulturalScore = session.CulturalScore ?? 0,
                    AIFeedback = session.AIFeedback,
                    Improvements = session.Improvements,
                    Strengths = session.Strengths,
                    SessionDuration = session.Duration ,
                    TotalMessages = messages.Count,

                    // Truyền dữ liệu phân tích chi tiết (cho Frontend hiển thị đẹp)
                    FluentAnalysis = evaluationResult.FluentAnalysis,
                    GrammarAnalysis = evaluationResult.GrammarAnalysis,
                    VocabularyAnalysis = evaluationResult.VocabularyAnalysis,
                    CulturalAnalysis = evaluationResult.CulturalAnalysis,
                    SpecificObservations = evaluationResult.SpecificObservations,
                    PositivePatterns = evaluationResult.PositivePatterns,
                    AreasNeedingWork = evaluationResult.AreasNeedingWork,
                    ProgressSummary = evaluationResult.ProgressSummary
                };

                // 8. Gửi SignalR (Realtime notification)
                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("ConversationEvaluated", new
                    {
                        sessionId,
                        evaluation = resultDto // Gửi full DTO xuống client
                    });

                return resultDto;
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
                    "VI" => "Không thể đánh giá cuộc hội thoại",
                    _ => "Cannot evaluate conversation"
                };

                await _hubContext.Clients.Group($"User_{userId}")
                    .SendAsync("EvaluationError", new { sessionId, error = errorMessage });

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
                        LanguageName = session.Language?.LanguageName ?? string.Empty,
                        TopicName = session.Topic?.Name ?? string.Empty,
                        DifficultyLevel = session.DifficultyLevel,
                        CharacterRole = MakeConciseRole(session.AICharacterRole ?? string.Empty, session.Topic?.Name ?? string.Empty),
                        ScenarioDescription = session.GeneratedScenario ?? string.Empty,
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
                if (session == null) return null;

                var ll = await _unitOfWork.LearnerLanguages.GetByIdAsync(session.LearnerId);
                if (ll == null || ll.UserId != userId) return null;

                var messages = session.ConversationMessages?
                .OrderBy(m => m.SequenceOrder)
                .Select(MapToMessageDto)
                .ToList() ?? new List<ConversationMessageDto>();

                return new ConversationSessionDto
                {
                    SessionId = session.ConversationSessionID,
                    SessionName = session.SessionName,
                    LanguageName = session.Language?.LanguageName ?? string.Empty,
                    TopicName = session.Topic?.Name ?? string.Empty,
                    DifficultyLevel = session.DifficultyLevel,
                    CharacterRole = MakeConciseRole(session.AICharacterRole ?? string.Empty, session.Topic?.Name ?? string.Empty),
                    ScenarioDescription = session.GeneratedScenario ?? string.Empty,
                    Messages = messages,
                    Tasks = session.Tasks.OrderBy(t => t.TaskSequence).Select(MapToTaskDto).ToList(),
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

        public async Task<ConversationUsageDto> GetConversationUsageAsync(Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null) throw new ArgumentException("User not found");

                var now = TimeHelper.GetVietnamTime();
                // Load active subscription directly (avoid relying on navigation)
                var activeSubscription = await _unitOfWork.UserSubscriptions.FindAsync(
                    s => s.UserID == userId && s.IsActive && s.StartDate <= now && (s.EndDate == null || s.EndDate > now));

                var dailyLimit = activeSubscription?.ConversationQuota ?? user.DailyConversationLimit;

                // Determine subscriptionType: prefer active subscription, else infer by quota
                string subscriptionType;
                if (activeSubscription != null)
                {
                    subscriptionType = activeSubscription.SubscriptionType;
                }
                else
                {
                    subscriptionType = SubscriptionConstants.SubscriptionQuotas
 .FirstOrDefault(kv => kv.Value == dailyLimit).Key ?? SubscriptionConstants.FREE;
                }

                decimal? planPrice = null;
                string? planPriceVnd = null;
                if (!string.IsNullOrWhiteSpace(subscriptionType) && SubscriptionConstants.SubscriptionPrices.TryGetValue(subscriptionType, out var price))
                {
                    planPrice = price;
                    planPriceVnd = string.Format(new CultureInfo("vi-VN"), "{0:C0}", price).Replace("₫", "đ");
                }

                int? inferredQuota = null;
                if (activeSubscription == null)
                {
                    if (SubscriptionConstants.SubscriptionQuotas.TryGetValue(subscriptionType, out var q))
                    {
                        inferredQuota = (int?)q;
                    }
                }

                var dto = new ConversationUsageDto
                {
                    ConversationsUsedToday = user.ConversationsUsedToday,
                    DailyLimit = dailyLimit,
                    SubscriptionType = subscriptionType,
                    ResetDate = user.LastConversationResetDate.Date.AddDays(1),
                    HasActiveSubscription = activeSubscription != null,
                    CurrentPlan = activeSubscription?.SubscriptionType ?? subscriptionType,
                    PlanDailyQuota = activeSubscription?.ConversationQuota ?? inferredQuota,
                    PlanPrice = planPrice,
                    PlanPriceVndFormatted = planPriceVnd,
                    PlanStartDate = activeSubscription?.StartDate,
                    PlanEndDate = activeSubscription?.EndDate
                };

                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation usage for user {UserId}", userId);
                throw;
            }
        }

        // ===== Helpers =====

        private async Task<bool> CanStartConversationAsync(User user)
        {
            if (user == null) return false;

            var now = TimeHelper.GetVietnamTime();
            var activeSubscription = await _unitOfWork.UserSubscriptions.FindAsync(
                s => s.UserID == user.UserID && s.IsActive && s.StartDate <= now && (s.EndDate == null || s.EndDate > now));

            if (activeSubscription != null)
            {
                var quota = activeSubscription.ConversationQuota;
                if (user.LastConversationResetDate.Date < TimeHelper.GetVietnamTime().Date)
                {
                    user.ConversationsUsedToday = 0;
                    user.LastConversationResetDate = TimeHelper.GetVietnamTime();
                    await _unitOfWork.Users.UpdateAsync(user);
                    await _unitOfWork.SaveChangesAsync();
                }
                return user.ConversationsUsedToday < quota;
            }

            if (user.LastConversationResetDate.Date < TimeHelper.GetVietnamTime().Date)
            {
                user.ConversationsUsedToday = 0;
                user.LastConversationResetDate = TimeHelper.GetVietnamTime();
                await _unitOfWork.Users.UpdateAsync(user);
                await _unitOfWork.SaveChangesAsync();
            }
            return user.ConversationsUsedToday < user.DailyConversationLimit;
        }

        private List<ConversationTaskDto> NormalizeAndSelectTasks(string topicName, List<ConversationTaskDto> aiTasks)
        {
            var tasks = aiTasks ?? new List<ConversationTaskDto>();

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

            var unique = new HashSet<string>();
            var result = new List<ConversationTaskDto>(capacity: 3);

            foreach (var t in tasks.OrderBy(t => t.TaskSequence))
            {
                if (string.IsNullOrWhiteSpace(t.TaskDescription)) continue;

                var shortened = ShortenTask(t.TaskDescription);

                var normalizedDescription = NormalizeForDedup(shortened);
                var normalizedContext = NormalizeForDedup(t.TaskContext ?? string.Empty);
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
                    var normalizedContext = NormalizeForDedup(dt.TaskContext ?? string.Empty);
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

        private List<ConversationTaskDto> CreateDefaultTasksForTopic(string topicName)
        {
            return topicName.ToLower() switch
            {
                var t when t.Contains("interview") || t.Contains("phỏng vấn") || t.Contains("面接") || t.Contains("採用") => new List<ConversationTaskDto>
 {
 new() { TaskDescription = "Self-introduction and strengths", TaskSequence =1 },
 new() { TaskDescription = "Why our company", TaskSequence =2 },
 new() { TaskDescription = "Biggest technical challenge", TaskSequence =3 }
 },
                var t when t.Contains("lost luggage") || t.Contains("lost baggage") || t.Contains("baggage claim") || t.Contains("mất hành lý") || t.Contains("hành lý thất lạc") => new List<ConversationTaskDto>
 {
 new() { TaskDescription = "Report your missing bag and describe it", TaskSequence =1 },
 new() { TaskDescription = "Ask how and when it will be delivered", TaskSequence =2 },
 new() { TaskDescription = "Provide contact details clearly", TaskSequence =3 }
 },
                var t when t.Contains("restaurant") || t.Contains("ẩm thực") => new List<ConversationTaskDto>
 {
 new() { TaskDescription = "Ask the waiter for a recommendation", TaskSequence =1 },
 new() { TaskDescription = "Order your main course", TaskSequence =2 },
 new() { TaskDescription = "Ask for the bill", TaskSequence =3 }
 },
                _ => new List<ConversationTaskDto>
 {
 new() { TaskDescription = "Start the conversation", TaskSequence =1 },
 new() { TaskDescription = "Ask follow-up questions", TaskSequence =2 },
 new() { TaskDescription = "Express your thoughts", TaskSequence =3 }
 }
            };
        }

        private string ShortenTask(string description)
        {
            if (string.IsNullOrWhiteSpace(description)) return string.Empty;
            var s = description.Replace("\n", " ").Replace("\r", " ").Trim();

            foreach (var filler in new[] { "Please ", "Kindly ", "First, ", "Then, ", "Finally, ", "Next, " })
            {
                if (s.StartsWith(filler, StringComparison.OrdinalIgnoreCase))
                {
                    s = s.Substring(filler.Length).TrimStart();
                }
            }

            var periodIndex = s.IndexOf('.');
            if (periodIndex > 0 && s.Length > 70)
            {
                s = s.Substring(0, periodIndex).Trim();
            }

            if (s.Length > 70)
            {
                var words = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                s = string.Join(' ', words.Take(8));
            }

            s = s.Trim().TrimEnd('.', '!', '、', '。');
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
            var textToNormalize = pipeIndex > 0 ? description[..pipeIndex] : description;

            var lower = textToNormalize.ToLowerInvariant().Trim();
            var builder = new StringBuilder(lower.Length);
            foreach (var ch in lower)
            {
                if (char.IsLetterOrDigit(ch) || char.IsWhiteSpace(ch)) builder.Append(ch);
            }
            return string.Join(' ', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        }

        private async Task<RoleplayResponseDto> GenerateAIResponseAsync(ConversationSession session, string userMessage)
        {
            try
            {
                if (_geminiService != null)
                {
                    var enforcedSystemPrompt = $@"{session.GeneratedSystemPrompt}

IMPORTANT: You MUST respond ONLY in {session.Language?.LanguageName ?? "English"}.
Do NOT respond in any other language (including Vietnamese), regardless of the user's input language.
Always stick to the {session.GeneratedScenario} context and your role as {MakeConciseRole(session.AICharacterRole ?? string.Empty, session.Topic?.Name ?? string.Empty)}.
Always respond in {session.Language?.LanguageName ?? "English"} only.";

                    var responseDto = await _geminiService.GenerateResponseAsync(
                        enforcedSystemPrompt,
                        userMessage,
                        session.ConversationMessages?
                            .OrderBy(m => m.SequenceOrder)
                            .Select(m => $"{m.Sender}: {m.MessageContent}")
                            .ToList() ?? new List<string>(),
                        languageName: session.Language?.LanguageName ?? "English",
                        topic: session.Topic?.Name ?? "",
                        aiRoleName: session.AICharacterRole ?? "AI Partner"
                    );

                    // Làm sạch phần nội dung text bên trong DTO
                    responseDto.Content = CleanAIPrefix(responseDto.Content);

                    return responseDto;
                }

                // Fallback: Trả về DTO giả nếu service null
                return new RoleplayResponseDto
                {
                    Content = GetSimpleResponse(userMessage, session.Language?.LanguageCode ?? "EN")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating AI response");
                // Fallback: Trả về DTO giả nếu lỗi
                return new RoleplayResponseDto
                {
                    Content = GetDefaultResponse(session.Language?.LanguageCode ?? "EN")
                };
            }
        }
        private string CleanAIPrefix(string response)
        {
            if (string.IsNullOrWhiteSpace(response)) return response;
            
            // Remove common AI prefixes
            var prefixes = new[] { "AI: ", "AI:", "Assistant: ", "Assistant:", "Character: ", "Character:" };
            foreach (var prefix in prefixes)
            {
                if (response.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return response.Substring(prefix.Length).Trim();
                }
            }
            
            return response;
        }

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

        private string GetDefaultFirstMessage(string language, string topic, string level, string languageCode)
        {
            var isBasicLevel = level.Contains("A1") || level.Contains("N5") || level.Contains("HSK1");
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
            return pipeIndex > 0 ? text[..pipeIndex].Trim() : text.Trim();
        }

        private string GetDefaultRole(string topicName)
        {
            return topicName.ToLower() switch
            {
                var topic when topic.Contains("interview") || topic.Contains("phỏng vấn") || topic.Contains("面接") || topic.Contains("採用") => "Hiring Manager (Interviewer)",
                var topic when topic.Contains("lost luggage") || topic.Contains("lost baggage") || topic.Contains("baggage claim") || topic.Contains("mất hành lý") || topic.Contains("hành lý thất lạc") => "Baggage Service Agent",
                var topic when topic.Contains("restaurant") || topic.Contains("ẩm thực") => "Restaurant Server",
                var topic when topic.Contains("travel") || topic.Contains("du lịch") => "Local Guide",
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
                return "Hiring Manager (Interviewer)";
            }
            if (!string.IsNullOrWhiteSpace(aiRoleCandidate))
            {
                return aiRoleCandidate.Trim();
            }
            return GetDefaultRole(topicName);
        }

        private string MakeConciseRole(string aiRoleCandidate, string topicName)
        {
            if (string.IsNullOrWhiteSpace(aiRoleCandidate)) return GetDefaultRole(topicName);

            var role = aiRoleCandidate;
            // Cut bilingual part
            var pipeIndex = role.IndexOf('|');
            if (pipeIndex >= 0) role = role[..pipeIndex].Trim();

            // Remove narrative prefixes
            foreach (var prefix in new[] { "You are a ", "You are an ", "You're a ", "You're an " })
            {
                if (role.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    role = role.Substring(prefix.Length).Trim();
                    break;
                }
            }

            // Keep first sentence only
            var dot = role.IndexOf('.');
            if (dot > 0) role = role[..dot].Trim();

            // Normalize to short known labels by keywords
            var lower = role.ToLowerInvariant();
            if (lower.Contains("interview")) return "Hiring Manager (Interviewer)";
            if (lower.Contains("baggage") || lower.Contains("lost")) return "Baggage Service Agent";
            if (lower.Contains("waiter") || lower.Contains("server") || lower.Contains("restaurant")) return "Restaurant Server";
            if (lower.Contains("guide")) return "Local Guide";
            if (lower.Contains("shop")) return "Shop Assistant";
            if (lower.Contains("doctor") || lower.Contains("nurse") || lower.Contains("health")) return "Health Advisor";

            // Fallback if too long
            if (role.Length > 60) return GetDefaultRole(topicName);
            return role;
        }

        private string EnhanceResponseWithTranslationHint(string aiResponse, string userMessage, string targetLanguageCode)
        {
            if (!IsVietnamese(userMessage)) return aiResponse;
            if (targetLanguageCode.ToUpper() == "VI") return aiResponse;

            // Optional: compute hint but do not append
            _ = GetTranslationHint(userMessage, targetLanguageCode);
            return aiResponse;
        }

        private bool IsVietnamese(string text)
        {
            var vietnameseDiacritics = new[] { 'ả', 'ă', 'â', 'ấ', 'ầ', 'ẩ', 'ẫ', 'ậ', 'đ', 'ế', 'ề', 'ễ', 'ệ', 'ì', 'í', 'ỉ', 'ĩ', 'ị', 'ố', 'ồ', 'ổ', 'ỗ', 'ộ', 'ớ', 'ờ', 'ở', 'ỡ', 'ợ', 'ù', 'ú', 'ủ', 'ũ', 'ụ', 'ứ', 'ừ', 'ử', 'ữ', 'ự', 'ỳ', 'ý', 'ỷ', 'ỹ', 'ỵ' };
            return text.ToLower().Any(c => vietnameseDiacritics.Contains(c));
        }

        private string GetTranslationHint(string vietnameseText, string targetLanguageCode)
        {
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
                    var t = _geminiService.TranslateTextAsync(vietnameseText, "Vietnamese", languageName);
                    t.Wait(5000);
                    return t.IsCompletedSuccessfully ? (t.Result ?? vietnameseText) : GetSimpleTranslation(vietnameseText, targetLanguageCode);
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
            var translations = new Dictionary<string, Dictionary<string, string>>
 {
 { "EN", new Dictionary<string, string> { { "xin chào", "Hello" }, { "tạm biệt", "Goodbye" }, { "cảm ơn", "Thank you" }, { "không", "No" }, { "có", "Yes" }, { "tôi tên là", "My name is" }, { "bạn khỏe không", "How are you" }, { "rất vui gặp bạn", "Nice to meet you" } } },
 { "JP", new Dictionary<string, string> { { "xin chào", "こんにちは" }, { "tạm biệt", "さようなら" }, { "cảm ơn", "ありがとう" }, { "không", "いいえ" }, { "có", "はい" }, { "tôi tên là", "私の名前は" }, { "bạn khỏe không", "元気ですか" }, { "rất vui gặp bạn", "お会いして嬉しいです" } } },
 { "ZH", new Dictionary<string, string> { { "xin chào", "你好" }, { "tạm biệt", "再见" }, { "cảm ơn", "谢谢" }, { "không", "不" }, { "có", "是" }, { "tôi tên là", "我叫" }, { "bạn khỏe không", "你好吗" }, { "rất vui gặp bạn", "很高兴认识你" } } }
 };
            var key = targetLanguageCode.ToUpper();
            if (!translations.ContainsKey(key)) return vietnameseText;
            var lowerText = vietnameseText.ToLower();
            foreach (var phrase in translations[key]) if (lowerText.Contains(phrase.Key)) return phrase.Value;
            return vietnameseText;
        }

        private async Task<ConversationEvaluationDto> GenerateEvaluationAsync(ConversationSession session)
        {
            try
            {
                var messageCount = session.ConversationMessages?.Count(m => m.Sender == MessageSender.User) ?? 0;
                var duration = (int)(TimeHelper.GetVietnamTime() - session.StartedAt).TotalSeconds;

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

Provide detailed qualitative analysis.";

                    var aiEvaluation = await _geminiService.EvaluateConversationAsync(
    evaluationPrompt,
    session.Language?.LanguageName ?? "English" 
);
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
                            SessionDuration = duration,
                            // NEW: Detailed analysis
                            FluentAnalysis = aiEvaluation.FluentAnalysis,
                            GrammarAnalysis = aiEvaluation.GrammarAnalysis,
                            VocabularyAnalysis = aiEvaluation.VocabularyAnalysis,
                            CulturalAnalysis = aiEvaluation.CulturalAnalysis,
                            SpecificObservations = aiEvaluation.SpecificObservations,
                            PositivePatterns = aiEvaluation.PositivePatterns,
                            AreasNeedingWork = aiEvaluation.AreasNeedingWork,
                            ProgressSummary = aiEvaluation.ProgressSummary
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "AI evaluation failed, using simple evaluation");
            }

            var mc = session.ConversationMessages?.Count(m => m.Sender == MessageSender.User) ?? 0;
            var dur = (int)(TimeHelper.GetVietnamTime() - session.StartedAt).TotalSeconds;
            return GenerateSimpleEvaluation(session, mc, dur);
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
                AIFeedback = GenerateFeedbackMessage(messageCount, duration, session.Language?.LanguageName ?? string.Empty, languageCode),
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
                ? "Great start! Try sending more messages to get better practice."
                : messageCount < 3
                ? $"Good! You've started the conversation in {languageName}. Try speaking more."
                : messageCount < 10
                ? "Very good! You're maintaining the conversation."
                : "Excellent! You've had a very active conversation. Keep practicing!",
                "JP" => messageCount == 0
                ? "素晴らしいスタートです！もっとメッセージを送ってみてください。"
                : messageCount < 3
                ? $"いいですね！{languageName}で会話を始めました。もっと話してみましょう。"
                : messageCount < 10
                ? "とても良いです！会話を続けています。"
                : "素晴らしい！とても活発な会話ができました。",
                "ZH" => messageCount == 0
                ? "很好的开始！试着发送更多消息以更好地练习。"
                : messageCount < 3
                ? $"很好！你已经开始用{languageName}交谈。多说一些。"
                : messageCount < 10
                ? "非常好！你在维持对话。"
                : "优秀！你进行了很活跃的对话。继续练习！",
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
 difficultyLevel.Contains("A1") ? "Practice basic pronunciation" : "Try more complex sentence structures"
 },
                "JP" => new[]
                {
 messageCount < 5 ? "もっと長い文を話してみましょう" : "より多くの語彙を使ってみましょう",
 "質問を増やして会話を続けましょう",
 difficultyLevel.Contains("N5") ? "基本的な発音を練習しましょう" : "より複雑な文型を使ってみましょう"
 },
                "ZH" => new[]
                {
 messageCount < 5 ? "尝试说更长的句子" : "使用更丰富的词汇",
 "提出更多问题以保持对话",
 difficultyLevel.Contains("HSK1") ? "练习基础发音" : "尝试使用更复杂的句型"
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
 messageCount >= 10 ? "Active participation and good flow" : (messageCount >= 5 ? "Good responses" : "Positive learning attitude"),
 duration > 600 ? "Sustained conversation" : (duration > 300 ? "Good perseverance" : "Took initiative"),
 "Willing to practice and improve"
 },
                "JP" => new[]
                {
 messageCount >= 10 ? "積極的な参加と良い会話の流れ" : (messageCount >= 5 ? "適切な回答" : "前向きな学習姿勢"),
 duration > 600 ? "長時間の会話維持" : (duration > 300 ? "粘り強さ" : "主体性"),
 "練習に前向き"
 },
                "ZH" => new[]
                {
 messageCount >= 10 ? "积极参与并保持良好对话" : (messageCount >= 5 ? "回答得当" : "积极的学习态度"),
 duration > 600 ? "能维持较长时间对话" : (duration > 300 ? "有毅力" : "主动性强"),
 "愿意练习和提高"
 },
                _ => new[] { "Great effort", "Good practice", "Positive attitude" }
            };
            return string.Join(". ", strengths.Take(3)) + ".";
        }

        // ===== Mapper Methods =====
        
        private ConversationSessionDto MapToConversationSessionDto(ConversationSession session, List<ConversationMessage> messages)
        {
            return new ConversationSessionDto
            {
                SessionId = session.ConversationSessionID,
                SessionName = session.SessionName,
                LanguageName = session.Language?.LanguageName ?? string.Empty,
                TopicName = session.Topic?.Name ?? string.Empty,
                DifficultyLevel = session.DifficultyLevel,
                CharacterRole = MakeConciseRole(session.AICharacterRole ?? string.Empty, session.Topic?.Name ?? string.Empty),
                ScenarioDescription = session.GeneratedScenario ?? string.Empty,
                Messages = messages.Select(MapToMessageDto).ToList(),
                Tasks = session.Tasks
                    .GroupBy(t => t.TaskID)
                    .Select(g => g.First())
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
                AudioPublicId = message.AudioPublicId,
                AudioDuration = message.AudioDuration,
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

        private string GetSimpleResponse(string userMessage, string languageCode)
        {
            var responses = languageCode.ToUpper() switch
            {
                "EN" => new[]
                {
                    "That's interesting! Can you tell me more?",
                    "I understand. What do you think?",
                    "Great! How do you feel about that?"
                },
                "JP" => new[]
                {
                    "それは面白いですね！もう少し詳しく教えてください。",
                    "なるほど。どう思いますか？",
                    "いいですね！どう感じますか？"
                },
                "ZH" => new[]
                {
                    "这很有趣！你能多说一点吗？",
                    "我明白了。你怎么看？",
                    "很好！你对此感觉如何？"
                },
                _ => new[]
                {
                    "That's interesting! Can you tell me more?",
                    "I understand. What do you think?",
                    "Great! How do you feel about that?"
                }
            };
            var random = new Random();
            return responses[random.Next(responses.Length)];
        }

        private string GetDefaultResponse(string languageCode)
        {
            return languageCode.ToUpper() switch
            {
                "EN" => "Anyway, let's get back to what we were talking about.",
                "JP" => "さて、話に戻りましょう。",
                "ZH" => "总之，我们还是回到刚才的话题吧.",
                _ => "Let's continue our conversation."
            };
        }
    }
}