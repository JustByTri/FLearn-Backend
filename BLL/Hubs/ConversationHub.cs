using BLL.IServices.Coversation;
using BLL.IServices.Upload;
using Common.DTO.Conversation;
using DAL.Helpers;
using DAL.Type;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Hangfire;
using BLL.Background;
using BLL.Services.AI;

namespace BLL.Hubs
{
    [Authorize]
    public class ConversationHub : Hub
    {
        private readonly IConversationPartnerService _conversationService;
        private readonly ILogger<ConversationHub> _logger;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly IBackgroundJobClient _jobs;
        private readonly ITranscriptionService _transcribe;

        public ConversationHub(
             IConversationPartnerService conversationService,
             ICloudinaryService cloudinaryService,
             ILogger<ConversationHub> logger,
             IBackgroundJobClient jobs,
             ITranscriptionService transcribe)
        {
            _conversationService = conversationService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _jobs = jobs;
            _transcribe = transcribe;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"User_{userId}");

                _logger.LogInformation("🔗 User {UserId} connected to ConversationHub with ConnectionId {ConnectionId}",
                    userId, Context.ConnectionId);
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!string.IsNullOrEmpty(userId))
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"User_{userId}");

                _logger.LogInformation("🔌 User {UserId} disconnected from ConversationHub", userId);
            }

            await base.OnDisconnectedAsync(exception);
        }

        /// <summary>
        /// Join vào conversation room cụ thể
        /// </summary>
        public async Task JoinConversationRoom(string sessionId)
        {
            try
            {
                var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var session = await _conversationService.GetConversationSessionAsync(userId, Guid.Parse(sessionId));
                if (session != null)
                {
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{sessionId}");

                    await Clients.Group($"Conversation_{sessionId}")
                        .SendAsync("UserJoinedRoom", new { userId, sessionId, joinedAt = TimeHelper.GetVietnamTime()});

                    _logger.LogInformation("🚪 User {UserId} joined conversation room {SessionId}", userId, sessionId);
                }
                else
                {
                    await Clients.Caller.SendAsync("Error", "Session not found or access denied");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error joining conversation room {SessionId}", sessionId);
                await Clients.Caller.SendAsync("Error", "Không thể tham gia phòng trò chuyện");
            }
        }

        /// <summary>
        /// Leave conversation room
        /// </summary>
        public async Task LeaveConversationRoom(string sessionId)
        {
            try
            {
                var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{sessionId}");

                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("UserLeftRoom", new { userId, sessionId, leftAt = TimeHelper.GetVietnamTime()});

                _logger.LogInformation("🚪 User {UserId} left conversation room {SessionId}", userId, sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error leaving conversation room {SessionId}", sessionId);
            }
        }

        /// <summary>
        /// Gửi tin nhắn real-time
        /// </summary>
        public async Task SendMessageToConversation(string sessionId, string messageContent, string messageType = "Text")
        {
            try
            {
                var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Thông báo user đang typing
                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("UserStoppedTyping", new { userId });

                var request = new SendMessageRequestDto
                {
                    SessionId = Guid.Parse(sessionId),
                    MessageContent = messageContent,
              MessageType = Enum.Parse<MessageType>(messageType)
                };

                // Gửi user message trước
                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("MessageReceived", new
                    {
                        messageId = Guid.NewGuid(),
                        sessionId,
                        sender = "User",
                        content = messageContent,
                        messageType,
                        timestamp = DateTime.UtcNow,
                        sequenceOrder = -1 
                    });

                // Hiển thị AI typing
                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("AIStartedTyping", new { sessionId });

                // Gửi qua service để xử lý AI response
                var aiResponse = await _conversationService.SendMessageAsync(userId, request);

                // Gửi AI response
                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("AIMessageReceived", new
                    {
                        messageId = aiResponse.MessageId,
                        sessionId,
                        sender = "AI",
                        content = aiResponse.MessageContent,
                        messageType = aiResponse.MessageType.ToString(),
                        timestamp = aiResponse.SentAt,
                        sequenceOrder = aiResponse.SequenceOrder
                    });

                // Tắt AI typing
                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("AIStoppedTyping", new { sessionId });

                _logger.LogInformation("💬 Message sent in conversation {SessionId} by user {UserId}", sessionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending message to conversation {SessionId}", sessionId);
                await Clients.Caller.SendAsync("Error", "Không thể gửi tin nhắn");
            }
        }

        /// <summary>
        /// Typing indicators
        /// </summary>
        public async Task StartTyping(string sessionId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await Clients.OthersInGroup($"Conversation_{sessionId}")
                .SendAsync("UserStartedTyping", new { userId, sessionId });
        }

        public async Task StopTyping(string sessionId)
        {
            var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            await Clients.OthersInGroup($"Conversation_{sessionId}")
                .SendAsync("UserStoppedTyping", new { userId, sessionId });
        }
        /// <summary>
        /// 🎤 Voice message handling với real-time upload
        /// </summary>
        public async Task SendVoiceMessageToConversation(string sessionId, string base64Audio, string mimeType, int duration)
        {
            try
            {
                var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Decode audio bytes
                var commaIdx = base64Audio.IndexOf(',');
                if (commaIdx >= 0) base64Audio = base64Audio.Substring(commaIdx + 1); // strip data: prefix if present
                var audioBytes = Convert.FromBase64String(base64Audio);
                var fileName = $"voice_{DateTime.UtcNow:yyyyMMddHHmmss}.wav";
                var contentType = string.IsNullOrWhiteSpace(mimeType) ? "audio/wav" : mimeType;

                // Resolve session and STT locale with multi-language support
                var session = await _conversationService.GetConversationSessionAsync(userId, Guid.Parse(sessionId));
                string? sttLocale = null;
                string lnameRaw = session?.LanguageName ?? string.Empty;
                string NormalizeMulti(string s){ if(string.IsNullOrWhiteSpace(s)) return string.Empty; var lower=s.ToLowerInvariant(); var sb=new StringBuilder(lower.Length); foreach(var ch in lower) if(char.IsLetterOrDigit(ch)||char.IsWhiteSpace(ch)) sb.Append(ch); var cleaned=sb.ToString(); while(cleaned.Contains("  ")) cleaned=cleaned.Replace("  "," "); return cleaned.Trim(); }
                var lnameNorm = NormalizeMulti(lnameRaw);
                bool hasEnglish = lnameNorm.Contains("english") || lnameNorm.Contains("tieng anh") || lnameNorm.Contains("anh");
                bool hasJapanese = lnameNorm.Contains("japanese") || lnameNorm.Contains("tieng nhat") || lnameNorm.Contains("nihon") || lnameNorm.Contains("nippon") || lnameNorm.Contains("jp") || lnameNorm.Contains("nhat");
                bool hasChinese = lnameNorm.Contains("chinese") || lnameNorm.Contains("tieng trung") || lnameNorm.Contains("trung") || lnameNorm.Contains("zh") || lnameNorm.Contains("han");
                int matchedCount = (hasEnglish?1:0)+(hasJapanese?1:0)+(hasChinese?1:0);
                if (matchedCount <= 1)
                {
                    if (hasEnglish) sttLocale = "en-US"; else if (hasJapanese) sttLocale = "ja-JP"; else if (hasChinese) sttLocale = "zh-CN";
                }
                _logger.LogInformation("[Hub] STT locale resolved: {Locale} from LanguageName='{Name}' (normalized='{Norm}', multi={Multi})", sttLocale ?? "(auto)", lnameRaw, lnameNorm, matchedCount>1);

                // Transcribe quickly
                var transcript = await _transcribe.TranscribeAsync(audioBytes, fileName, contentType, sttLocale);
                if (string.IsNullOrWhiteSpace(transcript)) transcript = "[Voice Message]";

                // Notify group that a voice message was received (upload queued)
                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("VoiceMessageReceived", new
                    {
                        sessionId,
                        sender = "User",
                        audioUrl = (string?)null,
                        duration,
                        transcript,
                        upload = new { queued = true },
                        timestamp = DateTime.UtcNow
                    });

                // Process AI response using transcript
                var request = new SendMessageRequestDto
                {
                    SessionId = Guid.Parse(sessionId),
                    MessageContent = transcript,
                    MessageType = MessageType.Audio,
                    AudioDuration = duration
                };
                var aiResponse = await _conversationService.SendMessageAsync(userId, request);

                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("AIMessageReceived", new
                    {
                        messageId = aiResponse.MessageId,
                        sessionId,
                        sender = "AI",
                        content = aiResponse.MessageContent,
                        messageType = aiResponse.MessageType.ToString(),
                        timestamp = aiResponse.SentAt,
                        sequenceOrder = aiResponse.SequenceOrder
                    });

                // Enqueue background upload to Cloudinary and attach
                _jobs.Enqueue<VoiceUploadJob>(job => job.UploadAudioAndAttachAsync(Guid.Parse(sessionId), userId, audioBytes, fileName, contentType, duration));

                _logger.LogInformation("🎤 Voice (fast) sent in conversation {SessionId} by user {UserId}; upload queued", sessionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending voice message (fast) through SignalR");
                await Clients.Caller.SendAsync("Error", "Không thể gửi tin nhắn voice");
            }
        }
        /// <summary>
        /// Voice message handling
        /// </summary>
        public async Task SendVoiceMessage(string sessionId, string audioUrl, int duration)
        {
            try
            {
                var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var request = new SendMessageRequestDto
                {
                    SessionId = Guid.Parse(sessionId),
                    MessageContent = "[Voice Message]",
                    MessageType = MessageType.Audio,
                    AudioUrl = audioUrl,
                    AudioDuration = duration
                };

                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("VoiceMessageReceived", new
                    {
                        sessionId,
                        sender = "User",
                        audioUrl,
                        duration,
                        timestamp = DateTime.UtcNow
                    });

                // Process with AI (có thể convert voice to text trước)
                var aiResponse = await _conversationService.SendMessageAsync(userId, request);

                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("AIMessageReceived", new
                    {
                        messageId = aiResponse.MessageId,
                        sessionId,
                        sender = "AI",
                        content = aiResponse.MessageContent,
                        messageType = aiResponse.MessageType.ToString(),
                        timestamp = aiResponse.SentAt
                    });

                _logger.LogInformation("🎤 Voice message sent in conversation {SessionId}", sessionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error sending voice message");
                await Clients.Caller.SendAsync("Error", "Không thể gửi tin nhắn voice");
            }
        }

        /// <summary>
        /// End conversation and get evaluation
        /// </summary>
        public async Task EndConversation(string sessionId)
        {
            try
            {
                var userId = Guid.Parse(Context.User!.FindFirstValue(ClaimTypes.NameIdentifier)!);

                var evaluation = await _conversationService.EndConversationAsync(userId, Guid.Parse(sessionId));

                await Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("ConversationEnded", new
                    {
                        sessionId,
                        evaluation = new
                        {
                            overallScore = evaluation.OverallScore,
                            fluentScore = evaluation.FluentScore,
                            grammarScore = evaluation.GrammarScore,
                            vocabularyScore = evaluation.VocabularyScore,
                            culturalScore = evaluation.CulturalScore,
                            feedback = evaluation.AIFeedback,
                            improvements = evaluation.Improvements,
                            strengths = evaluation.Strengths,
                            totalMessages = evaluation.TotalMessages,
                            sessionDuration = evaluation.SessionDuration
                        },
                        endedAt = DateTime.UtcNow
                    });

                _logger.LogInformation("🏁 Conversation {SessionId} ended by user {UserId}", sessionId, userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error ending conversation {SessionId}", sessionId);
                await Clients.Caller.SendAsync("Error", "Không thể kết thúc cuộc trò chuyện");
            }
        }
    }
}

