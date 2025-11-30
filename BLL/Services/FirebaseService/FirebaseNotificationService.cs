using FirebaseAdmin;
using FirebaseAdmin.Messaging;

namespace BLL.Services.FirebaseService
{
    public class FirebaseNotificationService
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
    }
}
