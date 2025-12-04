using BLL.IServices.Auth;
using BLL.IServices.FirebaseService;
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
            var firebaseNotificationService = scope.ServiceProvider.GetRequiredService<IFirebaseNotificationService>();

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

                        // ✅ TẠO RefundRequest TỰ ĐỘNG CHO HỌC VIÊN
                        await CreateRefundRequestsForInsufficientClass(teacherClass, unitOfWork, firebaseNotificationService, emailService);

                        // Cập nhật trạng thái lớp
                        teacherClass.Status = ClassStatus.Cancelled_InsufficientStudents;
                        teacherClass.UpdatedAt = DateTime.UtcNow;

                        await unitOfWork.SaveChangesAsync();

                        _logger.LogInformation(
                            "❌ Class {ClassId} cancelled due to insufficient students. RefundRequests created for {Count} students.",
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

        /// <summary>
        /// Tạo RefundRequest tự động cho học viên khi lớp bị hủy do không đủ người
        /// (Đồng bộ với logic TeacherClassService.ExecuteCancellationAsync)
        /// </summary>
        private async Task CreateRefundRequestsForInsufficientClass(
            TeacherClass teacherClass,
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService firebaseNotificationService,
            IEmailService emailService)
        {
            var paidEnrollments = teacherClass.Enrollments
                .Where(e => e.Status == EnrollmentStatus.Paid)
                .ToList();

            _logger.LogInformation(
                "🔄 Creating RefundRequests for {Count} students in class {ClassId}",
                paidEnrollments.Count,
                teacherClass.ClassID
            );

            int successCount = 0;
            int failureCount = 0;

            foreach (var enrollment in paidEnrollments)
            {
                try
                {
                    // Tạo RefundRequest
                    var refundRequest = new RefundRequest
                    {
                        RefundRequestID = Guid.NewGuid(),
                        EnrollmentID = enrollment.EnrollmentID,
                        ClassID = teacherClass.ClassID,
                        StudentID = enrollment.StudentID,
                        RequestType = RefundRequestType.ClassCancelled_InsufficientStudents,
                        Reason = $"Class cancelled due to insufficient students ({teacherClass.CurrentEnrollments}/{MIN_STUDENTS})",
                        RefundAmount = enrollment.AmountPaid,
                        Status = RefundRequestStatus.Pending,

                        // Để trống - học viên cần cập nhật sau
                        BankName = string.Empty,
                        BankAccountNumber = string.Empty,
                        BankAccountHolderName = string.Empty,

                        RequestedAt = DateTime.UtcNow,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    await unitOfWork.RefundRequests.CreateAsync(refundRequest);

                    // Cập nhật trạng thái enrollment
                    enrollment.Status = EnrollmentStatus.PendingRefund;
                    enrollment.UpdatedAt = DateTime.UtcNow;
                    await unitOfWork.ClassEnrollments.UpdateAsync(enrollment);

                    // Gửi FCM notification
                    if (enrollment.Student != null && !string.IsNullOrEmpty(enrollment.Student.FcmToken))
                    {
                        try
                        {
                            await firebaseNotificationService.SendNotificationAsync(
                                enrollment.Student.FcmToken,
                                "Lớp học đã bị hủy ❌",
                                $"Lớp '{teacherClass.Title}' đã bị hủy do không đủ học viên. Vui lòng cập nhật thông tin ngân hàng để nhận hoàn tiền.",
                                new Dictionary<string, string>
                                {
                                    { "type", "class_cancelled_refund_required" },
                                    { "refundRequestId", refundRequest.RefundRequestID.ToString() },
                                    { "classId", teacherClass.ClassID.ToString() },
                                    { "className", teacherClass.Title ?? "Lớp học" },
                                    { "reason", "insufficient_students" }
                                }
                            );

                            _logger.LogInformation("[FCM] ✅ Sent notification to student {StudentId}", enrollment.StudentID);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[FCM] ❌ Failed to send notification to student {StudentId}", enrollment.StudentID);
                        }
                    }

                    // 📧 GỬI EMAIL CHO HỌC VIÊN
                    if (enrollment.Student != null && !string.IsNullOrEmpty(enrollment.Student.Email))
                    {
                        try
                        {
                            await emailService.SendRefundRequestInstructionAsync(
                                enrollment.Student.Email,
                                enrollment.Student.UserName,
                                teacherClass.Title ?? "Lớp học",
                                teacherClass.StartDateTime,
                                $"Lớp không đủ số lượng học viên tối thiểu ({MIN_STUDENTS} người)"
                            );

                            _logger.LogInformation("[EMAIL] ✅ Sent refund email to {Email}", enrollment.Student.Email);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[EMAIL] ❌ Failed to send email to {Email}", enrollment.Student.Email);
                        }
                    }

                    successCount++;
                    _logger.LogInformation(
                        "✅ Created RefundRequest {RefundRequestId} for student {StudentId}",
                        refundRequest.RefundRequestID,
                        enrollment.StudentID
                    );
                }
                catch (Exception ex)
                {
                    failureCount++;
                    _logger.LogError(
                        ex,
                        "❌ Failed to create RefundRequest for student {StudentId} in class {ClassId}",
                        enrollment.StudentID,
                        teacherClass.ClassID
                    );
                }
            }

            _logger.LogInformation(
                "📊 RefundRequest creation summary for class {ClassId}: {Success} created, {Failure} failed",
                teacherClass.ClassID,
                successCount,
                failureCount
            );
        }
    }
}
