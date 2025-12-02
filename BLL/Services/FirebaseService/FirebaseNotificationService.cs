using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using BLL.IServices.FirebaseService;

namespace BLL.Services.FirebaseService
{
    public class FirebaseNotificationService : IFirebaseNotificationService
    {
        public async Task SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("[FCM Warning] FirebaseApp is not initialized. Check firebase-admin-sdk.json");
                return;
            }

            var message = new Message()
            {
                Token = deviceToken,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            try
            {
                string response = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                Console.WriteLine($"[FCM] Sent successfully: {response}");
            }
            catch (FirebaseMessagingException ex)
            {
                Console.WriteLine($"[FCM] Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM] General Error: {ex.Message}");
            }
        }

        // Thông báo đăng ký lớp thành công
        public async Task SendClassRegistrationSuccessNotificationAsync(string deviceToken, string className, DateTime classStartTime)
        {
            var title = "Đăng ký lớp học thành công! 🎉";
            var body = $"Bạn đã đăng ký thành công lớp '{className}'. Lớp học bắt đầu vào {classStartTime:dd/MM/yyyy HH:mm}.";
            var data = new Dictionary<string, string>
            {
                { "type", "class_registration_success" },
                { "className", className },
                { "startTime", classStartTime.ToString("o") }
            };

            await SendNotificationAsync(deviceToken, title, body, data);
        }

        // Thông báo trước lớp 1 giờ
        public async Task SendClassReminderOneHourNotificationAsync(string deviceToken, string className, DateTime classStartTime)
        {
            var title = "Lớp học sắp bắt đầu! ⏰";
            var body = $"Lớp '{className}' sẽ bắt đầu trong 1 giờ nữa ({classStartTime:HH:mm}).";
            var data = new Dictionary<string, string>
            {
                { "type", "class_reminder_1hour" },
                { "className", className },
                { "startTime", classStartTime.ToString("o") }
            };

            await SendNotificationAsync(deviceToken, title, body, data);
        }

        // Thông báo trước lớp 5 phút
        public async Task SendClassReminderFiveMinutesNotificationAsync(string deviceToken, string className, DateTime classStartTime)
        {
            var title = "Lớp học sắp bắt đầu ngay! 🔔";
            var body = $"Lớp '{className}' sẽ bắt đầu trong 5 phút nữa. Hãy chuẩn bị sẵn sàng!";
            var data = new Dictionary<string, string>
            {
                { "type", "class_reminder_5min" },
                { "className", className },
                { "startTime", classStartTime.ToString("o") }
            };

            await SendNotificationAsync(deviceToken, title, body, data);
        }

        // Thông báo lớp bị hủy
        public async Task SendClassCancellationNotificationAsync(string deviceToken, string className, string reason = null)
        {
            var title = "Lớp học đã bị hủy ❌";
            var body = string.IsNullOrEmpty(reason) 
                ? $"Lớp '{className}' đã bị hủy. Vui lòng liên hệ để biết thêm chi tiết."
                : $"Lớp '{className}' đã bị hủy. Lý do: {reason}";
            var data = new Dictionary<string, string>
            {
                { "type", "class_cancelled" },
                { "className", className },
                { "reason", reason ?? "" }
            };

            await SendNotificationAsync(deviceToken, title, body, data);
        }

        // Gửi thông báo cho nhiều thiết bị
        public async Task SendMulticastNotificationAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string> data)
        {
            if (FirebaseApp.DefaultInstance == null)
            {
                Console.WriteLine("[FCM Warning] FirebaseApp is not initialized.");
                return;
            }

            if (deviceTokens == null || !deviceTokens.Any())
            {
                Console.WriteLine("[FCM Warning] No device tokens provided.");
                return;
            }

            var message = new MulticastMessage()
            {
                Tokens = deviceTokens,
                Notification = new Notification()
                {
                    Title = title,
                    Body = body
                },
                Data = data
            };

            try
            {
                var response = await FirebaseMessaging.DefaultInstance.SendEachForMulticastAsync(message);
                Console.WriteLine($"[FCM] Multicast sent: {response.SuccessCount} successful, {response.FailureCount} failed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FCM] Multicast Error: {ex.Message}");
            }
        }
    }
}
