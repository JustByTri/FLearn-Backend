using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using BLL.IServices.FirebaseService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BLL.Services.FirebaseService
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        private readonly string _defaultIconUrl;
        private readonly string _baseUrl;
        private readonly ILogger<FirebaseNotificationService> _logger;

        public FirebaseNotificationService(IConfiguration configuration, ILogger<FirebaseNotificationService> logger)
        {
            _defaultIconUrl = "https://res.cloudinary.com/dzo85c2kz/image/upload/v1764168994/logooo2_zanl1e.png";
            _baseUrl = configuration["AppSettings:BaseUrl"] ?? "https://flearn.com";
            _logger = logger;
        }

        // === MOBILE NOTIFICATIONS ===

        public async Task SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("[FCM] FirebaseApp is not initialized");
                return;
            }

            if (string.IsNullOrEmpty(deviceToken))
            {
                _logger.LogWarning("[FCM] Device token is null or empty");
                return;
            }

            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification() { Title = title, Body = body },
                Data = data
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("[FCM] ✅ Sent: {Response}", response);
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError(ex, "[FCM] ❌ Error: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] ❌ General Error");
            }
        }

        public async Task SendClassRegistrationSuccessNotificationAsync(string deviceToken, string className, DateTime classStartTime)
        {
            await SendNotificationAsync(deviceToken,
                "Đăng ký lớp học thành công! 🎉",
                $"Bạn đã đăng ký thành công lớp '{className}'. Lớp bắt đầu vào {classStartTime:dd/MM/yyyy HH:mm}.",
                new Dictionary<string, string>
                {
                    { "type", "class_registration_success" },
                    { "className", className },
                    { "startTime", classStartTime.ToString("o") }
                });
        }

        public async Task SendClassReminderOneHourNotificationAsync(string deviceToken, string className, DateTime classStartTime)
        {
            await SendNotificationAsync(deviceToken,
                "Lớp học sắp bắt đầu! ⏰",
                $"Lớp '{className}' sẽ bắt đầu trong 1 giờ nữa ({classStartTime:HH:mm}).",
                new Dictionary<string, string>
                {
                    { "type", "class_reminder_1hour" },
                    { "className", className }
                });
        }

        public async Task SendClassReminderFiveMinutesNotificationAsync(string deviceToken, string className, DateTime classStartTime)
        {
            await SendNotificationAsync(deviceToken,
                "Lớp học sắp bắt đầu ngay! 🔔",
                $"Lớp '{className}' sẽ bắt đầu trong 5 phút nữa!",
                new Dictionary<string, string>
                {
                    { "type", "class_reminder_5min" },
                    { "className", className }
                });
        }

        public async Task SendClassCancellationNotificationAsync(string deviceToken, string className, string? reason = null)
        {
            var body = string.IsNullOrEmpty(reason)
                ? $"Lớp '{className}' đã bị hủy."
                : $"Lớp '{className}' đã bị hủy. Lý do: {reason}";

            await SendNotificationAsync(deviceToken,
                "Lớp học đã bị hủy ❌",
                body,
                new Dictionary<string, string>
                {
                    { "type", "class_cancelled" },
                    { "className", className },
                    { "reason", reason ?? "" }
                });
        }

        public async Task SendMulticastNotificationAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string> data)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("[FCM] FirebaseApp is not initialized");
                return;
            }

            var validTokens = deviceTokens?.Where(t => !string.IsNullOrEmpty(t)).ToList();
            if (validTokens == null || !validTokens.Any())
            {
                _logger.LogWarning("[FCM] No valid tokens");
                return;
            }

            var message = new MulticastMessage()
            {
                Tokens = validTokens,
                Notification = new Notification() { Title = title, Body = body },
                Data = data
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("[FCM] Multicast: {Success}/{Total} success", response.SuccessCount, validTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM] Multicast Error");
            }
        }

        // === WEB PUSH NOTIFICATIONS ===

        public async Task SendWebPushNotificationAsync(
            string webToken,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            string? clickActionUrl = null)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("[FCM-Web] FirebaseApp is not initialized");
                return;
            }

            if (string.IsNullOrEmpty(webToken))
            {
                _logger.LogWarning("[FCM-Web] Web token is empty");
                return;
            }

            var message = new Message()
            {
                Token = webToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body,
                    ImageUrl = _defaultIconUrl
                },
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = title,
                        Body = body,
                        Icon = _defaultIconUrl,
                        Badge = _defaultIconUrl,
                        RequireInteraction = true,
                        Tag = data?.GetValueOrDefault("type") ?? "flearn"
                    },
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = clickActionUrl ?? _baseUrl
                    }
                },
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("[FCM-Web] ✅ Sent: {Response}", response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM-Web] ❌ Error");
            }
        }

        public async Task SendWebPushMulticastAsync(
            List<string> webTokens,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            string? clickActionUrl = null)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                _logger.LogWarning("[FCM-Web] FirebaseApp is not initialized");
                return;
            }

            var validTokens = webTokens?.Where(t => !string.IsNullOrEmpty(t)).ToList();
            if (validTokens == null || !validTokens.Any())
            {
                _logger.LogWarning("[FCM-Web] No valid tokens");
                return;
            }

            var message = new MulticastMessage()
            {
                Tokens = validTokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body,
                    ImageUrl = _defaultIconUrl
                },
                Webpush = new WebpushConfig()
                {
                    Notification = new WebpushNotification()
                    {
                        Title = title,
                        Body = body,
                        Icon = _defaultIconUrl,
                        RequireInteraction = true,
                        Tag = data?.GetValueOrDefault("type") ?? "flearn"
                    },
                    FcmOptions = new WebpushFcmOptions()
                    {
                        Link = clickActionUrl ?? _baseUrl
                    }
                },
                Data = data ?? new Dictionary<string, string>()
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                _logger.LogInformation("[FCM-Web] Multicast: {Success}/{Total}", response.SuccessCount, validTokens.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[FCM-Web] Multicast Error");
            }
        }

        // ============================================
        // TEACHER NOTIFICATIONS
        // ============================================

        public async Task SendNewEnrollmentNotificationToTeacherAsync(
            string teacherToken,
            string studentName,
            string className,
            int currentEnrollments,
            int maxStudents)
        {
            await SendWebPushNotificationAsync(
                teacherToken,
                "Học viên mới đăng ký! 🎓",
                $"{studentName} vừa đăng ký lớp '{className}'. ({currentEnrollments}/{maxStudents})",
                new Dictionary<string, string>
                {
                    { "type", "new_enrollment" },
                    { "studentName", studentName },
                    { "className", className }
                },
                $"{_baseUrl}/teacher/classes"
            );
        }

        public async Task SendClassFullNotificationToTeacherAsync(
            string teacherToken,
            string className,
            int totalStudents)
        {
            await SendWebPushNotificationAsync(
                teacherToken,
                "Lớp học đã đủ người! 🎉",
                $"Lớp '{className}' đã có đủ {totalStudents} học viên!",
                new Dictionary<string, string>
                {
                    { "type", "class_full" },
                    { "className", className }
                },
                $"{_baseUrl}/teacher/classes"
            );
        }

        public async Task SendCancellationRequestResultToTeacherAsync(
            string teacherToken,
            string className,
            bool isApproved,
            string? reason = null)
        {
            var title = isApproved ? "Yêu cầu hủy lớp được duyệt ✅" : "Yêu cầu hủy lớp bị từ chối ❌";
            var body = isApproved
                ? $"Yêu cầu hủy lớp '{className}' đã được phê duyệt."
                : $"Yêu cầu hủy lớp '{className}' bị từ chối. Lý do: {reason ?? "Không có"}";

            await SendWebPushNotificationAsync(
                teacherToken,
                title,
                body,
                new Dictionary<string, string>
                {
                    { "type", "cancellation_result" },
                    { "className", className },
                    { "isApproved", isApproved.ToString() }
                },
                $"{_baseUrl}/teacher/classes"
            );
        }

        public async Task SendNewSubmissionNotificationToTeacherAsync(
            string teacherToken,
            string studentName,
            string exerciseName,
            string courseName)
        {
            await SendWebPushNotificationAsync(
                teacherToken,
                "Học viên nộp bài mới! 📝",
                $"{studentName} đã nộp bài '{exerciseName}' trong khóa '{courseName}'.",
                new Dictionary<string, string>
                {
                    { "type", "new_submission" },
                    { "studentName", studentName },
                    { "exerciseName", exerciseName },
                    { "courseName", courseName }
                },
                $"{_baseUrl}/teacher/grading"
            );
        }

        public async Task SendPayoutResultToTeacherAsync(
            string teacherToken,
            decimal amount,
            bool isApproved,
            string? reason = null)
        {
            var title = isApproved ? "Yêu cầu rút tiền thành công! 💰" : "Yêu cầu rút tiền bị từ chối ❌";
            var body = isApproved
                ? $"Yêu cầu rút {amount:N0}đ đã được xử lý. Tiền sẽ được chuyển vào tài khoản của bạn."
                : $"Yêu cầu rút {amount:N0}đ bị từ chối. Lý do: {reason ?? "Không có"}. Tiền đã được hoàn lại ví.";

            await SendWebPushNotificationAsync(
                teacherToken,
                title,
                body,
                new Dictionary<string, string>
                {
                    { "type", "payout_result" },
                    { "amount", amount.ToString("N0") },
                    { "isApproved", isApproved.ToString() }
                },
                $"{_baseUrl}/teacher/wallet"
            );
        }

        // ============================================
        // STUDENT NOTIFICATIONS
        // ============================================

        public async Task SendGradingResultToStudentAsync(
            string studentToken,
            string exerciseName,
            string courseName,
            int? score,
            string? feedback = null)
        {
            var scoreText = score.HasValue ? $"Điểm: {score}/100" : "Đã chấm";
            var body = $"Bài '{exerciseName}' trong khóa '{courseName}' đã được chấm. {scoreText}";

            await SendNotificationAsync(
                studentToken,
                "Bài tập đã được chấm! 📊",
                body,
                new Dictionary<string, string>
                {
                    { "type", "grading_result" },
                    { "exerciseName", exerciseName },
                    { "courseName", courseName },
                    { "score", score?.ToString() ?? "" },
                    { "feedback", feedback ?? "" }
                });
        }

        public async Task SendRefundResultToStudentAsync(
            string studentToken,
            string className,
            decimal amount,
            bool isApproved,
            string? reason = null)
        {
            var title = isApproved ? "Hoàn tiền thành công! 💰" : "Yêu cầu hoàn tiền bị từ chối ❌";
            var body = isApproved
                ? $"Đơn hoàn tiền lớp '{className}' ({amount:N0}đ) đã được xử lý. Tiền sẽ được chuyển vào tài khoản của bạn."
                : $"Đơn hoàn tiền lớp '{className}' bị từ chối. Lý do: {reason ?? "Không có"}";

            await SendNotificationAsync(
                studentToken,
                title,
                body,
                new Dictionary<string, string>
                {
                    { "type", "refund_result" },
                    { "className", className },
                    { "amount", amount.ToString("N0") },
                    { "isApproved", isApproved.ToString() }
                });
        }

        public async Task SendApplicationResultToUserAsync(
            string userToken,
            string languageName,
            bool isApproved,
            string? reason = null)
        {
            var title = isApproved 
                ? "Chúc mừng! Đơn ứng tuyển được duyệt 🎉" 
                : "Đơn ứng tuyển bị từ chối ❌";
            var body = isApproved
                ? $"Đơn ứng tuyển giáo viên {languageName} của bạn đã được phê duyệt! Bạn có thể bắt đầu tạo lớp học."
                : $"Đơn ứng tuyển giáo viên {languageName} bị từ chối. Lý do: {reason ?? "Không có"}";

            await SendNotificationAsync(
                userToken,
                title,
                body,
                new Dictionary<string, string>
                {
                    { "type", "application_result" },
                    { "languageName", languageName },
                    { "isApproved", isApproved.ToString() }
                });
        }

        // ============================================
        // MANAGER NOTIFICATIONS
        // ============================================

        public async Task SendNewCancellationRequestToManagerAsync(
            List<string> managerTokens,
            string teacherName,
            string className,
            string reason)
        {
            await SendWebPushMulticastAsync(
                managerTokens,
                "Yêu cầu hủy lớp mới 📋",
                $"GV {teacherName} yêu cầu hủy lớp '{className}'. Lý do: {reason}",
                new Dictionary<string, string>
                {
                    { "type", "new_cancellation_request" },
                    { "teacherName", teacherName },
                    { "className", className }
                },
                $"{_baseUrl}/manager/dashboard"
            );
        }

        // ============================================
        // ADMIN NOTIFICATIONS
        // ============================================

        public async Task SendNewRefundRequestToAdminAsync(
            List<string> adminTokens,
            string studentName,
            string className,
            decimal amount)
        {
            await SendWebPushMulticastAsync(
                adminTokens,
                "Đơn hoàn tiền mới 💰",
                $"{studentName} yêu cầu hoàn tiền lớp '{className}'. Số tiền: {amount:N0}đ",
                new Dictionary<string, string>
                {
                    { "type", "new_refund_request" },
                    { "studentName", studentName },
                    { "amount", amount.ToString("N0") }
                },
                $"{_baseUrl}/admin/refunds"
            );
        }

        public async Task SendNewPayoutRequestToAdminAsync(
            List<string> adminTokens,
            string teacherName,
            decimal amount)
        {
            await SendWebPushMulticastAsync(
                adminTokens,
                "Yêu cầu rút tiền mới 💸",
                $"GV {teacherName} yêu cầu rút {amount:N0}đ.",
                new Dictionary<string, string>
                {
                    { "type", "new_payout_request" },
                    { "teacherName", teacherName },
                    { "amount", amount.ToString("N0") }
                },
                $"{_baseUrl}/admin/payouts"
            );
        }

        public async Task SendNewTeacherApplicationToAdminAsync(
            List<string> adminTokens,
            string applicantName,
            string language)
        {
            await SendWebPushMulticastAsync(
                adminTokens,
                "Đơn ứng tuyển giáo viên mới 👨‍🏫",
                $"{applicantName} nộp đơn ứng tuyển giảng dạy {language}.",
                new Dictionary<string, string>
                {
                    { "type", "new_teacher_application" },
                    { "applicantName", applicantName },
                    { "language", language }
                },
                $"{_baseUrl}/manager/applications"
            );
        }
    }
}
