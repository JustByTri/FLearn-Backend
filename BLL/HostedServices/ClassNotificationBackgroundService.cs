using BLL.IServices.FirebaseService;
using DAL.DBContext;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.HostedServices
{
    public class ClassNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClassNotificationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(1); 
        private readonly HashSet<string> _sentOneHourNotifications = new();
        private readonly HashSet<string> _sentFiveMinuteNotifications = new();

        public ClassNotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<ClassNotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[ClassNotification] Background Service is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessClassNotificationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[ClassNotification] Error occurred while processing class notifications.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }

            _logger.LogInformation("[ClassNotification] Background Service is stopping.");
        }

        private async Task ProcessClassNotificationsAsync()
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var notificationService = scope.ServiceProvider.GetRequiredService<IFirebaseNotificationService>();

            var now = DateTime.UtcNow;
            var oneHourLater = now.AddHours(1).AddMinutes(2);
            var fiveMinutesLater = now.AddMinutes(6);

       
            var upcomingClasses = await dbContext.TeacherClasses
                .Include(tc => tc.Enrollments.Where(e => e.Status == EnrollmentStatus.Paid))
                    .ThenInclude(e => e.Student)
                .Where(tc => (tc.Status == ClassStatus.Published || tc.Status == ClassStatus.Scheduled) 
                    && tc.StartDateTime > now 
                    && tc.StartDateTime <= oneHourLater)
                .ToListAsync();

            foreach (var teacherClass in upcomingClasses)
            {
                var timeUntilStart = teacherClass.StartDateTime - now;
                var classKey = teacherClass.ClassID.ToString();

          
                if (timeUntilStart.TotalMinutes >= 59 && timeUntilStart.TotalMinutes <= 61)
                {
                    var oneHourKey = $"{classKey}_1hour";
                    if (!_sentOneHourNotifications.Contains(oneHourKey))
                    {
                        await SendReminderToStudents(teacherClass, notificationService, "1hour");
                        _sentOneHourNotifications.Add(oneHourKey);
                        _logger.LogInformation($"[ClassNotification] Sent 1-hour reminder for class {teacherClass.ClassID}");
                    }
                }

             
                if (timeUntilStart.TotalMinutes >= 4 && timeUntilStart.TotalMinutes <= 6)
                {
                    var fiveMinKey = $"{classKey}_5min";
                    if (!_sentFiveMinuteNotifications.Contains(fiveMinKey))
                    {
                        await SendReminderToStudents(teacherClass, notificationService, "5min");
                        _sentFiveMinuteNotifications.Add(fiveMinKey);
                        _logger.LogInformation($"[ClassNotification] Sent 5-minute reminder for class {teacherClass.ClassID}");
                    }
                }
            }

            // Dọn dẹp cache cũ
            CleanupOldNotifications(now);
        }

        private async Task SendReminderToStudents(TeacherClass teacherClass, IFirebaseNotificationService notificationService, string reminderType)
        {
            var paidEnrollments = teacherClass.Enrollments.Where(e => e.Status == EnrollmentStatus.Paid).ToList();

            foreach (var enrollment in paidEnrollments)
            {
                if (enrollment.Student == null || string.IsNullOrEmpty(enrollment.Student.FcmToken))
                {
                    continue;
                }

                try
                {
                    if (reminderType == "1hour")
                    {
                        await notificationService.SendClassReminderOneHourNotificationAsync(
                            enrollment.Student.FcmToken, 
                            teacherClass.Title ?? "Lớp học", 
                            teacherClass.StartDateTime
                        );
                    }
                    else if (reminderType == "5min")
                    {
                        await notificationService.SendClassReminderFiveMinutesNotificationAsync(
                            enrollment.Student.FcmToken, 
                            teacherClass.Title ?? "Lớp học", 
                            teacherClass.StartDateTime
                        );
                    }

                    _logger.LogInformation($"[ClassNotification] Sent {reminderType} reminder to student {enrollment.StudentID} for class {teacherClass.ClassID}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"[ClassNotification] Failed to send {reminderType} reminder to student {enrollment.StudentID}");
                }
            }
        }

        private void CleanupOldNotifications(DateTime currentTime)
        {
        
            if (_sentOneHourNotifications.Count > 1000)
            {
                _sentOneHourNotifications.Clear();
                _logger.LogInformation("[ClassNotification] Cleared 1-hour notification cache");
            }

            if (_sentFiveMinuteNotifications.Count > 1000)
            {
                _sentFiveMinuteNotifications.Clear();
                _logger.LogInformation("[ClassNotification] Cleared 5-minute notification cache");
            }
        }
    }
}
