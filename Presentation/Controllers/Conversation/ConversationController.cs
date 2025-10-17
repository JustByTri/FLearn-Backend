using BLL.IServices.Coversation;
using BLL.IServices.Upload;
using Common.DTO.Conversation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Conversation
{
    [Route("api/conversation")]
    [ApiController]
    [Authorize]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationPartnerService _conversationService;
        private readonly ICloudinaryService _cloudinaryService;
        private readonly ILogger<ConversationController> _logger;

        public ConversationController(
            IConversationPartnerService conversationService,
            ICloudinaryService cloudinaryService,
            ILogger<ConversationController> logger)
        {
            _conversationService = conversationService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
        }

        /// <summary>
        /// Lấy danh sách ngôn ngữ có sẵn cho conversation
        /// </summary>
        [HttpGet("languages")]
        public async Task<IActionResult> GetAvailableLanguages()
        {
            try
            {
                var languages = await _conversationService.GetAvailableLanguagesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách ngôn ngữ thành công",
                    data = languages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available languages");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách ngôn ngữ",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách chủ đề có sẵn cho conversation
        /// </summary>
        [HttpGet("topics")]
        public async Task<IActionResult> GetAvailableTopics()
        {
            try
            {
                var topics = await _conversationService.GetAvailableTopicsAsync();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách chủ đề thành công",
                    data = topics
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available topics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách chủ đề",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Bắt đầu conversation session mới
        /// </summary>
        [HttpPost("start")]
        public async Task<IActionResult> StartConversation([FromBody] StartConversationRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var session = await _conversationService.StartConversationAsync(userId, request);

                return Ok(new
                {
                    success = true,
                    message = "Bắt đầu conversation thành công",
                    data = session
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting conversation");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi bắt đầu conversation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Gửi tin nhắn text trong conversation
        /// </summary>
        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] SendMessageRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var aiResponse = await _conversationService.SendMessageAsync(userId, request);

                return Ok(new
                {
                    success = true,
                    message = "Gửi tin nhắn thành công",
                    data = aiResponse
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi gửi tin nhắn",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🎤 Gửi voice message trong conversation với upload lên Cloudinary
        /// </summary>
        [HttpPost("send-voice")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SendVoiceMessage([FromForm] SendVoiceMessageFormDto formDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                // Validate audio file
                if (formDto.AudioFile == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Vui lòng chọn file audio"
                    });
                }

                var allowedTypes = new[] { "audio/mp3", "audio/wav", "audio/m4a", "audio/webm", "audio/mpeg", "audio/ogg" };
                if (!allowedTypes.Contains(formDto.AudioFile.ContentType.ToLower()))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Chỉ hỗ trợ file audio: MP3, WAV, M4A, WebM, OGG"
                    });
                }

                if (formDto.AudioFile.Length > 10 * 1024 * 1024) // 10MB
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "File audio không được vượt quá 10MB"
                    });
                }

                // 🎵 Upload audio to Cloudinary
                string audioUrl;
                string audioPublicId;
                try
                {
                    var audioFolder = $"conversations/{userId}/{formDto.SessionId}/voice_messages";
                    var uploadResult = await _cloudinaryService.UploadAudioAsync(formDto.AudioFile, audioFolder);

                    audioUrl = uploadResult.Url;
                    audioPublicId = uploadResult.PublicId;

                    _logger.LogInformation("✅ Voice message uploaded to Cloudinary: {PublicId} - {Url}",
                        audioPublicId, audioUrl);
                }
                catch (Exception uploadEx)
                {
                    _logger.LogError(uploadEx, "❌ Failed to upload voice message to Cloudinary for session {SessionId}",
                        formDto.SessionId);

                    return StatusCode(500, new
                    {
                        success = false,
                        message = "Lỗi upload voice message. Vui lòng thử lại.",
                        errorCode = "VOICE_UPLOAD_FAILED"
                    });
                }

                //  Create SendMessageRequestDto with voice data
                var request = new SendMessageRequestDto
                {
                    SessionId = formDto.SessionId,
                    MessageContent = "[Voice Message]", // Placeholder text
                    MessageType = DAL.Type.MessageType.Audio,
                    AudioUrl = audioUrl,
                    AudioPublicId = audioPublicId,
                    AudioDuration = formDto.AudioDuration
                };

                // Send through conversation service
                var aiResponse = await _conversationService.SendMessageAsync(userId, request);

                return Ok(new
                {
                    success = true,
                    message = "Gửi voice message thành công",
                    data = new
                    {
                        aiResponse,
                        voiceInfo = new
                        {
                            audioUrl,
                            audioPublicId,
                            audioDuration = formDto.AudioDuration,
                            fileSize = formDto.AudioFile.Length,
                            contentType = formDto.AudioFile.ContentType
                        }
                    }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending voice message");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi gửi voice message",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Kết thúc conversation và nhận đánh giá
        /// </summary>
        [HttpPost("{sessionId:guid}/end")]
        public async Task<IActionResult> EndConversation(Guid sessionId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var evaluation = await _conversationService.EndConversationAsync(userId, sessionId);

                return Ok(new
                {
                    success = true,
                    message = "Kết thúc conversation thành công",
                    data = evaluation
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ending conversation");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi kết thúc conversation",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy lịch sử conversations của user
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetConversationHistory()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var history = await _conversationService.GetUserConversationHistoryAsync(userId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy lịch sử conversation thành công",
                    data = history,
                    total = history.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation history");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy lịch sử",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy chi tiết một conversation session
        /// </summary>
        [HttpGet("{sessionId:guid}")]
        public async Task<IActionResult> GetConversationSession(Guid sessionId)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var session = await _conversationService.GetConversationSessionAsync(userId, sessionId);

                if (session == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy conversation session hoặc bạn không có quyền truy cập"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin session thành công",
                    data = session
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation session");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thông tin session",
                    error = ex.Message
                });
            }
        }
        [HttpGet("usage")]
        public async Task<IActionResult> GetConversationUsage()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var usage = await _conversationService.GetConversationUsageAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = usage
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting conversation usage");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error getting usage",
                    error = ex.Message
                });
            }
        }
    }
}
