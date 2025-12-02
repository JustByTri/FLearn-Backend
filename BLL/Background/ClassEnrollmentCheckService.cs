using BLL.IServices.Auth;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.Background
{
    public class ClassEnrollmentCheckService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClassEnrollmentCheckService> _logger;
        private const int MIN_STUDENTS = 3; // Số học sinh tối thiểu

        public ClassEnrollmentCheckService(
            IServiceProvider serviceProvider,
            ILogger<ClassEnrollmentCheckService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

            _logger.LogInformation("[LOGGER] Class Enrollment Check Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CheckClassEnrollments();

                    // Chạy mỗi 6 giờ
                    await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Class Enrollment Check Service");
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken); // Retry sau 30 phút nếu có lỗi
                }
            }
        }

        private async Task CheckClassEnrollments()
        {
            using var scope = _serviceProvider.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            try
            {
                var threeDaysFromNow = DateTime.UtcNow.AddDays(3);
                var threeDaysAgo = DateTime.UtcNow.AddDays(2.5);

                var upcomingClasses = await unitOfWork.TeacherClasses
                    .GetClassesStartingBetween(threeDaysAgo, threeDaysFromNow);

                _logger.LogInformation("📊 Checking {Count} upcoming classes for minimum enrollment", upcomingClasses.Count);

                foreach (var teacherClass in upcomingClasses)
                {
                    if (teacherClass.Status == ClassStatus.Cancelled_InsufficientStudents)
                        continue;

                    if (teacherClass.CurrentEnrollments < MIN_STUDENTS)
                    {
                        _logger.LogWarning(
                            "⚠️ Class {ClassId} ({ClassName}) has insufficient students: {Current}/{Min}",
                            teacherClass.ClassID,
                            teacherClass.Title,
                            teacherClass.CurrentEnrollments,
                            MIN_STUDENTS
                        );

                        // ✅ GỌI PHƯƠNG THỨC ĐÚNG - Hướng dẫn gửi đơn hoàn tiền
                        await SendRefundInstructionEmailsToStudents(teacherClass, emailService);

                        // Cập nhật trạng thái lớp
                        teacherClass.Status = ClassStatus.Cancelled_InsufficientStudents;
                        teacherClass.UpdatedAt = DateTime.UtcNow;

                        await unitOfWork.SaveChangesAsync();

                        _logger.LogInformation(
                            "❌ Class {ClassId} cancelled, refund instruction emails sent to {Count} students",
                            teacherClass.ClassID,
                            teacherClass.CurrentEnrollments
                        );
                    }
                    else
                    {
                        _logger.LogInformation(
                            "✅ Class {ClassId} ({ClassName}) has sufficient students: {Current}/{Min}",
                            teacherClass.ClassID,
                            teacherClass.Title,
                            teacherClass.CurrentEnrollments,
                            MIN_STUDENTS
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking class enrollments");
                throw;
            }
        }

        // ✅ GIỮ LẠI PHƯƠNG THỨC NÀY - Đã cập nhật để gửi hướng dẫn
        private async Task SendRefundInstructionEmailsToStudents(
            TeacherClass teacherClass,
            IEmailService emailService)
        {
            var paidEnrollments = teacherClass.Enrollments
                .Where(e => e.Status == EnrollmentStatus.Paid)
                .ToList();

            _logger.LogInformation(
                "📧 Sending refund instruction emails to {Count} students for class {ClassId}",
                paidEnrollments.Count,
                teacherClass.ClassID
            );

            int successCount = 0;
            int failureCount = 0;

            foreach (var enrollment in paidEnrollments)
            {
                try
                {
                    // ✅ GỌI PHƯƠNG THỨC ĐÚNG - Hướng dẫn gửi đơn
                    var success = await emailService.SendRefundRequestInstructionAsync(
                        enrollment.Student.Email,
                        enrollment.Student.FullName,
                        teacherClass.Title,
                        teacherClass.StartDateTime
                    );

                    if (success)
                    {
                        successCount++;
                        _logger.LogInformation(
                            "✉️ Refund instruction email sent to {Email} for class {ClassId}",
                            enrollment.Student.Email,
                            teacherClass.ClassID
                        );
                    }
                    else
                    {
                        failureCount++;
                        _logger.LogWarning(
                            "⚠️ Failed to send refund instruction email to {Email} for class {ClassId}",
                            enrollment.Student.Email,
                            teacherClass.ClassID
                        );
                    }
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(
                        ex,
                        "❌ Exception sending refund instruction email to {Email} for class {ClassId}",
                        enrollment.Student.Email,
                        teacherClass.ClassID
                    );
                }
            }

            _logger.LogInformation(
                "📊 Refund instruction emails summary for class {ClassId}: {Success} sent, {Failure} failed",
                teacherClass.ClassID,
                successCount,
                failureCount
            );
        }

    }

}
