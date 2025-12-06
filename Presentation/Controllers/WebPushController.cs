using DAL.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using BLL.IServices.FirebaseService;

namespace Presentation.Controllers
{
    [Route("api/web-push")]
    [ApiController]
    [Authorize]
    public class WebPushController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IFirebaseNotificationService _notificationService;
        private readonly ILogger<WebPushController> _logger;

        public WebPushController(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService notificationService,
            ILogger<WebPushController> logger)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _logger = logger;
        }

        /// <summary>
        /// Đăng ký FCM Token cho Web Push Notifications (Desktop)
        /// Dùng chung field FcmToken với Mobile
        /// </summary>
        /// <remarks>
        /// **Frontend cần làm:**
        /// 
        /// 1. Tạo file `public/firebase-messaging-sw.js`:
        /// ```javascript
        /// importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-app-compat.js');
        /// importScripts('https://www.gstatic.com/firebasejs/9.0.0/firebase-messaging-compat.js');
        /// 
        /// firebase.initializeApp({
        ///     apiKey: "YOUR_API_KEY",
        ///     projectId: "YOUR_PROJECT_ID",
        ///     messagingSenderId: "YOUR_SENDER_ID",
        ///     appId: "YOUR_APP_ID"
        /// });
        /// 
        /// firebase.messaging().onBackgroundMessage((payload) => {
        ///     self.registration.showNotification(payload.notification.title, {
        ///         body: payload.notification.body,
        ///         icon: '/logo.png'
        ///     });
        /// });
        /// ```
        /// 
        /// 2. Trong React/Vue app:
        /// ```javascript
        /// import { getMessaging, getToken } from 'firebase/messaging';
        /// 
        /// const messaging = getMessaging();
        /// const token = await getToken(messaging, { vapidKey: 'YOUR_VAPID_KEY' });
        /// 
        /// await fetch('/api/web-push/register', {
        ///     method: 'POST',
        ///     headers: { 'Authorization': 'Bearer ...', 'Content-Type': 'application/json' },
        ///     body: JSON.stringify({ token })
        /// });
        /// ```
        /// </remarks>
        [HttpPost("register")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> RegisterWebPushToken([FromBody] RegisterWebPushTokenDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var userId, out var error))
                    return error!;

                if (string.IsNullOrWhiteSpace(dto.Token))
                    return BadRequest(new { success = false, message = "Token is required" });

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // Dùng chung field FcmToken cho cả Mobile và Web
                user.FcmToken = dto.Token;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("[WebPush] ✅ Token registered for user {UserId}", userId);

                return Ok(new
                {
                    success = true,
                    message = "Web Push token đã được đăng ký thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebPush] Error registering token");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Hủy đăng ký Web Push Token (khi logout hoặc tắt notifications)
        /// </summary>
        [HttpPost("unregister")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> UnregisterWebPushToken()
        {
            try
            {
                if (!this.TryGetUserId(out var userId, out var error))
                    return error!;

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                user.FcmToken = null;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation("[WebPush] ✅ Token unregistered for user {UserId}", userId);

                return Ok(new
                {
                    success = true,
                    message = "Đã hủy đăng ký thông báo"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebPush] Error unregistering token");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// Kiểm tra trạng thái đăng ký Web Push
        /// </summary>
        [HttpGet("status")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetWebPushStatus()
        {
            try
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
                        isRegistered = !string.IsNullOrEmpty(user.FcmToken),
                        message = !string.IsNullOrEmpty(user.FcmToken) 
                            ? "Đã bật thông báo" 
                            : "Chưa bật thông báo"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebPush] Error getting status");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [TEST] Gửi test notification để verify Web Push hoạt động
        /// </summary>
        [HttpPost("test")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> SendTestNotification()
        {
            try
            {
                if (!this.TryGetUserId(out var userId, out var error))
                    return error!;

                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                if (string.IsNullOrEmpty(user.FcmToken))
                    return BadRequest(new
                    {
                        success = false,
                        message = "Bạn chưa bật thông báo. Vui lòng bật thông báo trước."
                    });

                await _notificationService.SendWebPushNotificationAsync(
                    user.FcmToken,
                    "Test thông báo 🔔",
                    "Nếu bạn thấy thông báo này, Web Push đã hoạt động!",
                    new Dictionary<string, string>
                    {
                        { "type", "test_notification" },
                        { "timestamp", DateTime.UtcNow.ToString("o") }
                    }
                );

                return Ok(new
                {
                    success = true,
                    message = "Đã gửi test notification! Kiểm tra desktop của bạn."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WebPush] Error sending test notification");
                return StatusCode(500, new { success = false, message = "Lỗi hệ thống" });
            }
        }
    }

    public class RegisterWebPushTokenDto
    {
        /// <summary>
        /// FCM Token lấy từ Firebase Messaging SDK trên browser
        /// </summary>
        public string Token { get; set; } = string.Empty;
    }
}
