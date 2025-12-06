using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BLL.IServices.FirebaseService;
using Presentation.Helpers;

namespace Presentation.Controllers
{
    /// <summary>
    /// Controller để test Web Push Notifications
    /// </summary>
    [Route("api/test-notification")]
    [ApiController]
    public class TestNotificationController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _notificationService;
        private readonly ILogger<TestNotificationController> _logger;

        public TestNotificationController(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService notificationService,
            ILogger<TestNotificationController> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// [DEBUG] Kiểm tra FCM Token của user hiện tại
        /// </summary>
        [HttpGet("my-token")]
        [Authorize]
        public async Task<IActionResult> GetMyToken()
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var user = await _unitOfWork.Users.GetByIdAsync(userId);
            if (user == null)
                return NotFound(new { success = false, message = "User not found" });

            return Ok(new
            {
                success = true,
                data = new
                {
                    userId = user.UserID,
                    userName = user.UserName,
                    hasFcmToken = !string.IsNullOrEmpty(user.FcmToken),
                    fcmTokenPreview = string.IsNullOrEmpty(user.FcmToken) 
                        ? null 
                        : user.FcmToken.Substring(0, Math.Min(50, user.FcmToken.Length)) + "..."
                }
            });
        }

        /// <summary>
        /// [DEBUG] Gửi test notification với token tùy chỉnh (không cần auth)
        /// Dùng để test trực tiếp với token từ browser
        /// </summary>
        [HttpPost("send-direct")]
        [AllowAnonymous]
        public async Task<IActionResult> SendDirectNotification([FromBody] DirectNotificationDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Token))
                return BadRequest(new { success = false, message = "Token is required" });

            try
            {
                _logger.LogInformation("[TEST] Sending direct notification to token: {TokenPreview}...", 
                    dto.Token.Substring(0, Math.Min(50, dto.Token.Length)));

                await _notificationService.SendWebPushNotificationAsync(
                    dto.Token,
                    dto.Title ?? "Test Notification 🔔",
                    dto.Body ?? "Đây là test notification từ FLearn!",
                    new Dictionary<string, string>
                    {
                        { "type", "test_direct" },
                        { "timestamp", DateTime.UtcNow.ToString("o") }
                    }
                );

                return Ok(new
                {
                    success = true,
                    message = "Đã gửi notification! Kiểm tra browser của bạn.",
                    note = "Nếu không thấy, kiểm tra: 1) Browser đã cho phép notifications chưa? 2) Token có đúng không?"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[TEST] Error sending direct notification");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Lỗi khi gửi notification",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// [DEBUG] Lấy hướng dẫn setup Web Push cho Frontend
        /// </summary>
        [HttpGet("setup-guide")]
        [AllowAnonymous]
        public IActionResult GetSetupGuide()
        {
            return Ok(new
            {
                success = true,
                message = "Hướng dẫn setup Web Push Notifications",
                steps = new[]
                {
                    "1. Tạo file public/firebase-messaging-sw.js (xem code bên dưới)",
                    "2. Khởi tạo Firebase trong app và lấy FCM token",
                    "3. Gọi POST /api/web-push/register với token",
                    "4. Gọi POST /api/web-push/test để test"
                },
                importantNote = "⚠️ Bạn KHÔNG THỂ test Web Push từ Swagger! " +
                    "Bạn cần có Frontend app (React/Vue/HTML) với Firebase SDK để lấy FCM token và nhận notifications."
            });
        }
    }

    public class DirectNotificationDto
    {
        /// <summary>
        /// FCM Token lấy từ browser (từ Firebase Messaging SDK)
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Tiêu đề notification (optional)
        /// </summary>
        public string? Title { get; set; }

        /// <summary>
        /// Nội dung notification (optional)
        /// </summary>
        public string? Body { get; set; }
    }
}
