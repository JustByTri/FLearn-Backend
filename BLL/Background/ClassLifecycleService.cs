using BLL.IServices.FirebaseService;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.Background
{
    /// <summary>
    /// Background Service quản lý vòng đời lớp học:
    /// 1. Tự động chuyển trạng thái: Published → InProgress → Finished → Completed_PendingPayout → Completed_Paid
    /// 2. Xử lý dispute window (3 ngày sau khi lớp kết thúc)
    /// 3. Chuyển tiền vào ví giáo viên (90% cho teacher, 10% platform fee)
    /// </summary>
    public class ClassLifecycleService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClassLifecycleService> _logger;

        // Platform fee: 10% (có thể config sau)
        private const decimal PLATFORM_FEE_PERCENTAGE = 0.10m;
        private const decimal TEACHER_PERCENTAGE = 0.90m;

        // Dispute window: 3 ngày sau khi lớp kết thúc
        private const int DISPUTE_WINDOW_DAYS = 3;

        public ClassLifecycleService(IServiceProvider serviceProvider, ILogger<ClassLifecycleService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            // Delay khởi động 2 phút để đợi các service khác sẵn sàng
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            _logger.LogInformation("🚀 [ClassLifecycle] Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                    var firebaseService = scope.ServiceProvider.GetRequiredService<IFirebaseNotificationService>();

                    // 1. Cập nhật lớp đang diễn ra (Published → InProgress)
                    await UpdateClassesToInProgress(unitOfWork);

                    // 2. Cập nhật lớp đã kết thúc (InProgress → Finished)
                    await UpdateClassesToFinished(unitOfWork);

                    // 3. Xử lý dispute window và chuyển sang Completed_PendingPayout
                    await HandleDisputeWindowAndMarkPendingPayout(unitOfWork);

                    // 4. Xử lý payout cho giáo viên (Completed_PendingPayout → Completed_Paid)
                    await ProcessTeacherPayouts(unitOfWork, firebaseService);

                    await unitOfWork.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [ClassLifecycle] Error in main loop");
                }

                // Chạy mỗi 15 phút
                await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
            }

            _logger.LogInformation("🛑 [ClassLifecycle] Service stopped");
        }

        /// <summary>
        /// Cập nhật lớp từ Published → InProgress khi đến giờ bắt đầu
        /// </summary>
        private async Task UpdateClassesToInProgress(IUnitOfWork unitOfWork)
        {
            try
            {
                var now = DateTime.UtcNow;

                var classesToStart = await unitOfWork.TeacherClasses.Query()
                    .Where(c => c.Status == ClassStatus.Published
                             && c.StartDateTime <= now
                             && c.EndDateTime > now)
                    .ToListAsync();

                foreach (var teacherClass in classesToStart)
                {
                    teacherClass.Status = ClassStatus.InProgress;
                    teacherClass.UpdatedAt = now;

                    _logger.LogInformation(
                        "▶️ [ClassLifecycle] Class {ClassId} ({Title}) started - Status: InProgress",
                        teacherClass.ClassID, teacherClass.Title);
                }

                if (classesToStart.Any())
                {
                    await unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ [ClassLifecycle] Updated {Count} classes to InProgress", classesToStart.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ClassLifecycle] Error updating classes to InProgress");
            }
        }

        /// <summary>
        /// Cập nhật lớp từ InProgress → Finished khi hết giờ
        /// </summary>
        private async Task UpdateClassesToFinished(IUnitOfWork unitOfWork)
        {
            try
            {
                var now = DateTime.UtcNow;

                var classesToFinish = await unitOfWork.TeacherClasses.Query()
                    .Where(c => c.Status == ClassStatus.InProgress
                             && c.EndDateTime <= now)
                    .ToListAsync();

                foreach (var teacherClass in classesToFinish)
                {
                    teacherClass.Status = ClassStatus.Finished;
                    teacherClass.UpdatedAt = now;

                    _logger.LogInformation(
                        "⏹️ [ClassLifecycle] Class {ClassId} ({Title}) finished - Status: Finished, Dispute window opened",
                        teacherClass.ClassID, teacherClass.Title);
                }

                if (classesToFinish.Any())
                {
                    await unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ [ClassLifecycle] Updated {Count} classes to Finished", classesToFinish.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ClassLifecycle] Error updating classes to Finished");
            }
        }

        /// <summary>
        /// Sau dispute window (3 ngày), chuyển lớp sang Completed_PendingPayout
        /// Nếu có dispute chưa giải quyết, giữ nguyên trạng thái Finished
        /// </summary>
        private async Task HandleDisputeWindowAndMarkPendingPayout(IUnitOfWork unitOfWork)
        {
            try
            {
                var now = DateTime.UtcNow;
                var disputeWindowEnd = now.AddDays(-DISPUTE_WINDOW_DAYS);

                var finishedClasses = await unitOfWork.TeacherClasses.Query()
                    .Include(c => c.Disputes)
                    .Include(c => c.Enrollments)
                    .Where(c => c.Status == ClassStatus.Finished
                             && c.EndDateTime <= disputeWindowEnd) // Đã qua 3 ngày
                    .ToListAsync();

                foreach (var teacherClass in finishedClasses)
                {
                    // Kiểm tra có dispute chưa giải quyết không
                    var hasUnresolvedDispute = teacherClass.Disputes?
                        .Any(d => d.Status == DisputeStatus.Open 
                               || d.Status == DisputeStatus.UnderReview
                               || d.Status == DisputeStatus.Submmitted) ?? false;

                    if (hasUnresolvedDispute)
                    {
                        _logger.LogWarning(
                            "⚠️ [ClassLifecycle] Class {ClassId} has unresolved disputes - holding payout",
                            teacherClass.ClassID);
                        continue;
                    }

                    // Kiểm tra có học viên đã thanh toán không
                    var paidEnrollments = teacherClass.Enrollments?
                        .Count(e => e.Status == DAL.Models.EnrollmentStatus.Paid 
                                 || e.Status == DAL.Models.EnrollmentStatus.Completed) ?? 0;

                    if (paidEnrollments == 0)
                    {
                        _logger.LogInformation(
                            "ℹ️ [ClassLifecycle] Class {ClassId} has no paid enrollments - marking as Completed_Paid directly",
                            teacherClass.ClassID);

                        teacherClass.Status = ClassStatus.Completed_Paid;
                        teacherClass.UpdatedAt = now;
                        continue;
                    }

                    // Chuyển sang Completed_PendingPayout
                    teacherClass.Status = ClassStatus.Completed_PendingPayout;
                    teacherClass.UpdatedAt = now;

                    _logger.LogInformation(
                        "💰 [ClassLifecycle] Class {ClassId} ({Title}) - Dispute window closed, Status: Completed_PendingPayout",
                        teacherClass.ClassID, teacherClass.Title);
                }

                if (finishedClasses.Any())
                {
                    await unitOfWork.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ClassLifecycle] Error handling dispute window");
            }
        }

        /// <summary>
        /// Xử lý payout cho giáo viên:
        /// - Tính tổng tiền từ enrollments
        /// - Trừ platform fee (10%)
        /// - Cộng vào ví giáo viên (90%)
        /// - Chuyển trạng thái sang Completed_Paid
        /// </summary>
        private async Task ProcessTeacherPayouts(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService)
        {
            try
            {
                var now = DateTime.UtcNow;

                var classesForPayout = await unitOfWork.TeacherClasses.Query()
                    .Include(c => c.Teacher)
                        .ThenInclude(t => t!.TeacherProfile)
                    .Include(c => c.Enrollments)
                    .Where(c => c.Status == ClassStatus.Completed_PendingPayout)
                    .ToListAsync();

                foreach (var teacherClass in classesForPayout)
                {
                    try
                    {
                        await ProcessSingleClassPayout(unitOfWork, firebaseService, teacherClass, now);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "❌ [ClassLifecycle] Error processing payout for class {ClassId}",
                            teacherClass.ClassID);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ClassLifecycle] Error in ProcessTeacherPayouts");
            }
        }

        /// <summary>
        /// Xử lý payout cho một lớp học cụ thể
        /// </summary>
        private async Task ProcessSingleClassPayout(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService firebaseService,
            TeacherClass teacherClass,
            DateTime now)
        {
            // 1. Tính tổng tiền từ các enrollment đã thanh toán
            var paidEnrollments = teacherClass.Enrollments?
                .Where(e => e.Status == DAL.Models.EnrollmentStatus.Paid 
                         || e.Status == DAL.Models.EnrollmentStatus.Completed)
                .ToList() ?? new List<ClassEnrollment>();

            if (!paidEnrollments.Any())
            {
                teacherClass.Status = ClassStatus.Completed_Paid;
                teacherClass.UpdatedAt = now;
                await unitOfWork.SaveChangesAsync();
                return;
            }

            var totalRevenue = paidEnrollments.Sum(e => e.AmountPaid);
            var platformFee = totalRevenue * PLATFORM_FEE_PERCENTAGE;
            var teacherPayout = totalRevenue * TEACHER_PERCENTAGE;

            _logger.LogInformation(
                "💵 [ClassLifecycle] Class {ClassId}: TotalRevenue={Total}, PlatformFee={Fee} (10%), TeacherPayout={Payout} (90%)",
                teacherClass.ClassID, totalRevenue, platformFee, teacherPayout);

            // 2. Lấy ví của giáo viên
            var teacherProfile = teacherClass.Teacher?.TeacherProfile;
            if (teacherProfile == null)
            {
                _logger.LogError(
                    "❌ [ClassLifecycle] Teacher profile not found for class {ClassId}",
                    teacherClass.ClassID);
                return;
            }

            var teacherWallet = await unitOfWork.Wallets.Query()
                .FirstOrDefaultAsync(w => w.TeacherId == teacherProfile.TeacherId);

            if (teacherWallet == null)
            {
                _logger.LogError(
                    "❌ [ClassLifecycle] Wallet not found for teacher {TeacherId}",
                    teacherProfile.TeacherId);
                return;
            }

            // 3. Cộng tiền vào ví giáo viên
            teacherWallet.TotalBalance += teacherPayout;
            teacherWallet.AvailableBalance += teacherPayout;
            teacherWallet.UpdatedAt = now;

            // 4. Tạo wallet transaction cho giáo viên
            var walletTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = teacherWallet.WalletId,
                TransactionType = DAL.Type.TransactionType.Payout,
                Amount = teacherPayout,
                ReferenceId = teacherClass.ClassID,
                ReferenceType = DAL.Type.ReferenceType.Class,
                Description = $"Thanh toán lớp học '{teacherClass.Title}' ({paidEnrollments.Count} học viên)",
                Status = DAL.Type.TransactionStatus.Succeeded,
                CreatedAt = now
            };

            await unitOfWork.WalletTransactions.AddAsync(walletTransaction);

            // 5. Tạo TeacherPayout record
            var teacherPayoutRecord = new TeacherPayout
            {
                TeacherPayoutId = Guid.NewGuid(),
                TeacherId = teacherProfile.TeacherId,
                ClassID = teacherClass.ClassID,
                PeriodStart = teacherClass.StartDateTime,
                PeriodEnd = teacherClass.EndDateTime,
                TotalLessons = 1,
                TotalEarnings = (double)totalRevenue,
                FinalAmount = (double)teacherPayout,
                Status = TeacherPayoutStatus.Paid,
                StaffId = Guid.Empty, // System auto-payout
                CreatedAt = now,
                UpdatedAt = now
            };

            await unitOfWork.TeacherPayouts.CreateAsync(teacherPayoutRecord);

            // 6. Cập nhật trạng thái enrollments
            foreach (var enrollment in paidEnrollments)
            {
                enrollment.Status = DAL.Models.EnrollmentStatus.Completed;
                enrollment.UpdatedAt = now;
            }

            // 7. Cập nhật trạng thái lớp
            teacherClass.Status = ClassStatus.Completed_Paid;
            teacherClass.UpdatedAt = now;

            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "✅ [ClassLifecycle] Payout completed for class {ClassId}: {Amount:N0} VND added to teacher wallet",
                teacherClass.ClassID, teacherPayout);

            // 8. Gửi thông báo cho giáo viên
            if (teacherClass.Teacher != null && !string.IsNullOrEmpty(teacherClass.Teacher.FcmToken))
            {
                try
                {
                    await firebaseService.SendNotificationAsync(
                        teacherClass.Teacher.FcmToken,
                        "Thanh toán lớp học thành công 💰",
                        $"Bạn đã nhận được {teacherPayout:N0} VND từ lớp '{teacherClass.Title}' ({paidEnrollments.Count} học viên)",
                        new Dictionary<string, string>
                        {
                            { "type", "class_payout_completed" },
                            { "classId", teacherClass.ClassID.ToString() },
                            { "amount", teacherPayout.ToString() },
                            { "walletTransactionId", walletTransaction.WalletTransactionId.ToString() }
                        }
                    );
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[FCM] Failed to send payout notification to teacher");
                }
            }
        }
    }
}
