using BLL.Hubs;
using BLL.IServices.Upload;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace BLL.Background
{
    public class VoiceUploadJob
    {
        private readonly ICloudinaryService _cloudinary;
        private readonly IUnitOfWork _unit;
        private readonly ILogger<VoiceUploadJob> _logger;
        private readonly IHubContext<ConversationHub> _hub;

        public VoiceUploadJob(ICloudinaryService cloudinary, IUnitOfWork unit, ILogger<VoiceUploadJob> logger, IHubContext<ConversationHub> hub)
        {
            _cloudinary = cloudinary;
            _unit = unit;
            _logger = logger;
            _hub = hub;
        }

        public async Task UploadAudioAndAttachAsync(Guid sessionId, Guid userId, byte[] audioBytes, string fileName, string contentType, int? duration)
        {
            try
            {
                await using var ms = new MemoryStream(audioBytes);
                var formFile = new FormFile(ms, 0, audioBytes.Length, "voice", fileName)
                {
                    Headers = new HeaderDictionary(),
                    ContentType = string.IsNullOrWhiteSpace(contentType) ? "audio/m4a" : contentType
                };

                var folder = $"conversations/{userId}/{sessionId}/voice_messages";
                var upload = await _cloudinary.UploadAudioAsync(formFile, folder);

                // Find the last user audio message in this session that has no audio URL yet
                var session = await _unit.ConversationSessions.GetSessionWithMessagesAsync(sessionId);
                var candidate = session?.ConversationMessages?
                .Where(m => m.Sender == MessageSender.User && m.MessageType == MessageType.Audio && string.IsNullOrEmpty(m.AudioUrl))
                .OrderByDescending(m => m.SentAt)
                .FirstOrDefault();

                if (candidate != null)
                {
                    candidate.AudioUrl = upload.Url;
                    candidate.AudioPublicId = upload.PublicId;
                    if (duration.HasValue) candidate.AudioDuration = duration;
                    await _unit.ConversationMessages.UpdateAsync(candidate);
                    await _unit.SaveChangesAsync();

                    // Notify clients that voice media has been attached
                    await _hub.Clients.Group($"Conversation_{sessionId}")
                    .SendAsync("VoiceAttached", new { messageId = candidate.ConversationMessageID, audioUrl = upload.Url, audioPublicId = upload.PublicId, audioDuration = duration });
                }
                else
                {
                    _logger.LogWarning("No pending user audio message to attach for session {SessionId}", sessionId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to upload/attach voice for session {SessionId}", sessionId);
            }
        }
    }
}
