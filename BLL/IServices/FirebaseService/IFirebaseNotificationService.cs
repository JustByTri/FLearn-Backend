namespace BLL.IServices.FirebaseService
{
    public interface IFirebaseNotificationService
    {
        Task SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data);
        Task SendClassRegistrationSuccessNotificationAsync(string deviceToken, string className, DateTime classStartTime);
        Task SendClassReminderOneHourNotificationAsync(string deviceToken, string className, DateTime classStartTime);
        Task SendClassReminderFiveMinutesNotificationAsync(string deviceToken, string className, DateTime classStartTime);
        Task SendClassCancellationNotificationAsync(string deviceToken, string className, string reason = null);
        Task SendMulticastNotificationAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string> data);
    }
}
