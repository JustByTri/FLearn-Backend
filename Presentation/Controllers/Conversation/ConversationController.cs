using BLL.IServices.Coversation;
using BLL.IServices.Upload;
using BLL.Background;
using BLL.Services.AI;
using Common.DTO.Conversation;
using Hangfire;
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
        private readonly IBackgroundJobClient _jobs;
        private readonly ITranscriptionService _transcribe;

        public ConversationController(
            IConversationPartnerService conversationService,
            ICloudinaryService cloudinaryService,
            ILogger<ConversationController> logger,
            IBackgroundJobClient jobs,
            ITranscriptionService transcribe)
        {
            _conversationService = conversationService;
            _cloudinaryService = cloudinaryService;
            _logger = logger;
            _jobs = jobs;
            _transcribe = transcribe;
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
        /// 🎤 Gửi voice message: STT realtime (Azure Speech) + phản hồi AI ngay, upload Cloudinary chạy nền
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
                    return BadRequest(new { success = false, message = "Vui lòng chọn file audio" });
                }

                var allowedTypes = new[] { "audio/mp3", "audio/wav", "audio/m4a", "audio/webm", "audio/mpeg", "audio/ogg" };
                if (!allowedTypes.Contains(formDto.AudioFile.ContentType.ToLower()))
                {
                    return BadRequest(new { success = false, message = "Chỉ hỗ trợ file audio: MP3, WAV, M4A, WebM, OGG" });
                }

                if (formDto.AudioFile.Length >10 *1024 *1024) //10MB
                {
                    return BadRequest(new { success = false, message = "File audio không được vượt quá10MB" });
                }

                // Ensure session exists and belongs to user (avoid wasted STT work)
                var session = await _conversationService.GetConversationSessionAsync(userId, formDto.SessionId);
                if (session == null)
                {
                    return NotFound(new { success = false, message = "Không tìm thấy session hoặc bạn không có quyền" });
                }

                // Buffer file for STT and background upload
                byte[] audioBytes;
                await using (var ms = new MemoryStream())
                {
                    await formDto.AudioFile.CopyToAsync(ms);
                    audioBytes = ms.ToArray();
                }
                var fileName = string.IsNullOrWhiteSpace(formDto.AudioFile.FileName) ? "audio.wav" : Path.GetFileName(formDto.AudioFile.FileName);
                var contentType = string.IsNullOrWhiteSpace(formDto.AudioFile.ContentType) ? "audio/wav" : formDto.AudioFile.ContentType;

                // Map session language name to STT locale (best-effort)
                string? sttLocale = null;
                var lname = (session.LanguageName ?? string.Empty).ToLowerInvariant();
                if (lname.Contains("english") || lname.Contains("tiếng anh") || lname.Contains("tieng anh")) sttLocale = "en-US";
                else if (lname.Contains("japanese") || lname.Contains("nihon") || lname.Contains("日本") || lname.Contains("jp") || lname.Contains("tiếng nhật") || lname.Contains("tieng nhat")) sttLocale = "ja-JP";
                else if (lname.Contains("chinese") || lname.Contains("中文") || lname.Contains("zh") || lname.Contains("tiếng trung") || lname.Contains("tieng trung") || lname.Contains("trung quốc")) sttLocale = "zh-CN";

                _logger.LogInformation("STT locale resolved: {Locale} from LanguageName='{Name}'", sttLocale ?? "(auto)", session.LanguageName);

                // Transcribe (if client didn't provide transcript)
                var transcript = string.IsNullOrWhiteSpace(formDto.Transcript)
                    ? await _transcribe.TranscribeAsync(audioBytes, fileName, contentType, sttLocale)
                    : formDto.Transcript;

                // Send to AI with transcript (fallback to placeholder if null)
                var request = new SendMessageRequestDto
                {
                    SessionId = formDto.SessionId,
                    MessageContent = string.IsNullOrWhiteSpace(transcript) ? "[Voice Message]" : transcript!,
                    MessageType = DAL.Type.MessageType.Audio,
                    AudioDuration = formDto.AudioDuration
                };
                var aiResponse = await _conversationService.SendMessageAsync(userId, request);

                // Background upload to Cloudinary
                _jobs.Enqueue<VoiceUploadJob>(job => job.UploadAudioAndAttachAsync(formDto.SessionId, userId, audioBytes, fileName, contentType, formDto.AudioDuration));

                return Ok(new
                {
                    success = true,
                    message = "Gửi voice thành công",
                    data = new { aiResponse, transcript, upload = new { queued = true, size = formDto.AudioFile.Length, contentType, fileName } }
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
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

        /// <summary>
        /// Lấy thông tin mức sử dụng cuộc hội thoại của người dùng
        /// </summary>
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
