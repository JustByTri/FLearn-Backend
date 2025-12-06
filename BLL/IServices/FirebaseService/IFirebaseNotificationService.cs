namespace BLL.IServices.FirebaseService
{
    public interface IFirebaseNotificationService
    {
        // === MOBILE NOTIFICATIONS ===
        Task SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string> data);
        Task SendClassRegistrationSuccessNotificationAsync(string deviceToken, string className, DateTime classStartTime);
        Task SendClassReminderOneHourNotificationAsync(string deviceToken, string className, DateTime classStartTime);
        Task SendClassReminderFiveMinutesNotificationAsync(string deviceToken, string className, DateTime classStartTime);
        Task SendClassCancellationNotificationAsync(string deviceToken, string className, string? reason = null);
        Task SendMulticastNotificationAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string> data);

        // === WEB PUSH NOTIFICATIONS (Desktop cho Teacher/Admin) ===
        /// <summary>
        /// G?i Web Push notification ??n browser (Desktop)
        /// </summary>
        Task SendWebPushNotificationAsync(
            string webToken,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            string? clickActionUrl = null);

        /// <summary>
        /// G?i Web Push ??n nhi?u browsers
        /// </summary>
        Task SendWebPushMulticastAsync(
            List<string> webTokens,
            string title,
            string body,
            Dictionary<string, string>? data = null,
            string? clickActionUrl = null);

        // === TEACHER NOTIFICATIONS ===
        /// <summary>
        /// Thông báo cho giáo viên khi có h?c viên ??ng ký l?p
        /// </summary>
        Task SendNewEnrollmentNotificationToTeacherAsync(
            string teacherToken, 
            string studentName, 
            string className, 
            int currentEnrollments, 
            int maxStudents);

        /// <summary>
        /// Thông báo cho giáo viên khi l?p ?? ng??i
        /// </summary>
        Task SendClassFullNotificationToTeacherAsync(
            string teacherToken, 
            string className, 
            int totalStudents);

        /// <summary>
        /// Thông báo cho giáo viên khi yêu c?u h?y l?p ???c duy?t/t? ch?i
        /// </summary>
        Task SendCancellationRequestResultToTeacherAsync(
            string teacherToken, 
            string className, 
            bool isApproved, 
            string? reason = null);

        // === MANAGER NOTIFICATIONS ===
        /// <summary>
        /// Thông báo cho manager khi có yêu c?u h?y l?p m?i c?n duy?t
        /// </summary>
        Task SendNewCancellationRequestToManagerAsync(
            List<string> managerTokens, 
            string teacherName, 
            string className, 
            string reason);

        // === ADMIN NOTIFICATIONS ===
        /// <summary>
        /// Thông báo cho admin khi có ??n hoàn ti?n m?i (?ã ?i?n STK)
        /// </summary>
        Task SendNewRefundRequestToAdminAsync(
            List<string> adminTokens, 
            string studentName, 
            string className, 
            decimal amount);

        /// <summary>
        /// Thông báo cho admin khi có ??n rút ti?n m?i t? giáo viên
        /// </summary>
        Task SendNewPayoutRequestToAdminAsync(
            List<string> adminTokens, 
            string teacherName, 
            decimal amount);

        /// <summary>
        /// Thông báo cho admin khi có ??n ?ng tuy?n giáo viên m?i
        /// </summary>
        Task SendNewTeacherApplicationToAdminAsync(
            List<string> adminTokens, 
            string applicantName, 
            string language);
    }
}
