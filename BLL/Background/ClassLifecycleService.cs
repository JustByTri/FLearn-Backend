using BLL.IServices.FirebaseService;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BLL.Background
{
    /// <summary>
    /// Service quản lý vòng đời lớp học (dùng cho Hangfire RecurringJob)
    /// </summary>
    public class ClassLifecycleHangfireJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClassLifecycleHangfireJob> _logger;

        // Platform fee: 10% (có thể config sau)
        private const decimal PLATFORM_FEE_PERCENTAGE = 0.10m;
        private const decimal TEACHER_PERCENTAGE = 0.90m;
        private const int DISPUTE_WINDOW_DAYS = 3;

        public ClassLifecycleHangfireJob(IServiceProvider serviceProvider, ILogger<ClassLifecycleHangfireJob> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        /// <summary>
        /// Phương thức này sẽ được Hangfire gọi định kỳ
        /// </summary>
        public async Task RunLifecycleJob()
        {
            var now = TimeHelper.GetVietnamTime();
            _logger.LogInformation("🔄 [ClassLifecycle] Hangfire job running at {Time} (Vietnam Time)", now);

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var firebaseService = scope.ServiceProvider.GetRequiredService<IFirebaseNotificationService>();

                // 1. Cập nhật lớp đang diễn ra (Published → InProgress)
                //    Hoặc xử lý lớp bị bỏ lỡ (Published → Finished)
                await UpdateClassesToInProgress(unitOfWork, now);

                // 2. Cập nhật lớp đã kết thúc (InProgress → Finished)
                await UpdateClassesToFinished(unitOfWork, now);

                // 3. Xử lý dispute window và chuyển sang Completed_PendingPayout
                //    - Nếu KHÔNG có dispute: 3 ngày sau khi lớp kết thúc
                //    - Nếu CÓ dispute: 3 ngày sau khi TẤT CẢ disputes được giải quyết
                await HandleDisputeWindowAndMarkPendingPayout(unitOfWork, firebaseService, now);

                // 4. Xử lý payout cho giáo viên (Completed_PendingPayout → Completed_Paid)
                await ProcessTeacherPayouts(unitOfWork, firebaseService, now);

                await unitOfWork.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ClassLifecycle] Error in Hangfire job");
            }
        }

        /// <summary>
        /// Cập nhật lớp từ Published → InProgress khi đến giờ bắt đầu
        /// HOẶC từ Published → Finished nếu đã qua cả giờ kết thúc (trường hợp bỏ lỡ)
        /// </summary>
        private async Task UpdateClassesToInProgress(IUnitOfWork unitOfWork, DateTime now)
        {
            try
            {
                // Lớp đang trong thời gian diễn ra (StartDateTime <= now < EndDateTime)
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
                        "▶️ [ClassLifecycle] Class {ClassId} ({Title}) started - Status: InProgress. StartTime: {Start}, Now: {Now}",
                        teacherClass.ClassID, teacherClass.Title, teacherClass.StartDateTime, now);
                }

                if (classesToStart.Any())
                {
                    await unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ [ClassLifecycle] Updated {Count} classes to InProgress", classesToStart.Count);
                }

                // ⚠️ XỬ LÝ TRƯỜNG HỢP BỎ LỠ: Lớp đã qua cả giờ kết thúc nhưng vẫn ở Published
                var missedClasses = await unitOfWork.TeacherClasses.Query()
                    .Where(c => c.Status == ClassStatus.Published
                             && c.EndDateTime <= now) // Đã qua giờ kết thúc
                    .ToListAsync();

                foreach (var teacherClass in missedClasses)
                {
                    teacherClass.Status = ClassStatus.Finished;
                    teacherClass.UpdatedAt = now;

                    _logger.LogWarning(
                        "⚠️ [ClassLifecycle] Class {ClassId} ({Title}) missed InProgress - Status: Published → Finished directly. EndTime: {End}, Now: {Now}",
                        teacherClass.ClassID, teacherClass.Title, teacherClass.EndDateTime, now);
                }

                if (missedClasses.Any())
                {
                    await unitOfWork.SaveChangesAsync();
                    _logger.LogInformation("✅ [ClassLifecycle] Updated {Count} missed classes to Finished", missedClasses.Count);
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
        private async Task UpdateClassesToFinished(IUnitOfWork unitOfWork, DateTime now)
        {
            try
            {
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
        /// Xử lý dispute window và chuyển lớp sang Completed_PendingPayout:
        /// - Nếu KHÔNG có dispute: 3 ngày sau khi lớp kết thúc
        /// - Nếu CÓ dispute đang pending: Giữ nguyên Finished, chờ giải quyết
        /// - Nếu CÓ dispute đã resolved: 3 ngày sau khi dispute cuối cùng được giải quyết
        /// </summary>
        private async Task HandleDisputeWindowAndMarkPendingPayout(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService, DateTime now)
        {
            try
            {
                var finishedClasses = await unitOfWork.TeacherClasses.Query()
                    .Include(c => c.Disputes)
                    .Include(c => c.Enrollments)
                    .Where(c => c.Status == ClassStatus.Finished)
                    .ToListAsync();

                foreach (var teacherClass in finishedClasses)
                {
                    var disputes = teacherClass.Disputes?.ToList() ?? new List<ClassDispute>();

                    // Kiểm tra có dispute CHƯA giải quyết không
                    var hasUnresolvedDispute = disputes.Any(d => 
                        d.Status == DisputeStatus.Open 
                        || d.Status == DisputeStatus.UnderReview
                        || d.Status == DisputeStatus.Submmitted);

                    if (hasUnresolvedDispute)
                    {
                        _logger.LogWarning(
                            "⏸️ [ClassLifecycle] Class {ClassId} has unresolved disputes - holding payout until resolved",
                            teacherClass.ClassID);
                        continue;
                    }

                    // Tính thời điểm có thể payout
                    DateTime payoutEligibleDate;

                    if (disputes.Any())
                    {
                        // CÓ dispute (đã resolved): 3 ngày sau khi dispute cuối cùng được giải quyết
                        var lastResolvedDate = disputes
                            .Where(d => d.ResolvedAt.HasValue)
                            .Select(d => d.ResolvedAt!.Value)
                            .DefaultIfEmpty(teacherClass.EndDateTime)
                            .Max();

                        payoutEligibleDate = lastResolvedDate.AddDays(DISPUTE_WINDOW_DAYS);

                        _logger.LogInformation(
                            "📋 [ClassLifecycle] Class {ClassId} has {Count} resolved disputes. Last resolved: {LastResolved}. Payout eligible: {EligibleDate}",
                            teacherClass.ClassID, disputes.Count, lastResolvedDate, payoutEligibleDate);
                    }
                    else
                    {
                        // KHÔNG có dispute: 3 ngày sau khi lớp kết thúc
                        payoutEligibleDate = teacherClass.EndDateTime.AddDays(DISPUTE_WINDOW_DAYS);
                    }

                    // Kiểm tra đã đủ thời gian chưa
                    if (now < payoutEligibleDate)
                    {
                        var daysRemaining = (payoutEligibleDate - now).TotalDays;
                        _logger.LogInformation(
                            "⏳ [ClassLifecycle] Class {ClassId} not yet eligible for payout. {Days:F1} days remaining",
                            teacherClass.ClassID, daysRemaining);
                        continue;
                    }

                    // Xử lý các dispute đã resolved (tạo RefundRequest nếu cần)
                    await ProcessResolvedDisputesForClass(unitOfWork, firebaseService, teacherClass, disputes, now);

                    // Kiểm tra có học viên đã thanh toán không (sau khi trừ các dispute refund)
                    var paidEnrollmentsCount = teacherClass.Enrollments?
                        .Count(e => e.Status == DAL.Models.EnrollmentStatus.Paid 
                                 || e.Status == DAL.Models.EnrollmentStatus.Completed) ?? 0;

                    if (paidEnrollmentsCount == 0)
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
        /// Xử lý các dispute đã resolved cho một lớp học:
        /// - Tạo RefundRequest cho học viên có dispute được chấp nhận
        /// - Cập nhật enrollment status
        /// </summary>
        private async Task ProcessResolvedDisputesForClass(
            IUnitOfWork unitOfWork,
            IFirebaseNotificationService firebaseService,
            TeacherClass teacherClass,
            List<ClassDispute> disputes,
            DateTime now)
        {
            var resolvedDisputes = disputes.Where(d => 
                d.Status == DisputeStatus.Resolved_Refunded 
                || d.Status == DisputeStatus.Resolved_PartialRefund).ToList();

            foreach (var dispute in resolvedDisputes)
            {
                // Kiểm tra đã tạo RefundRequest chưa
                var existingRefund = await unitOfWork.RefundRequests.Query()
                    .AnyAsync(r => r.EnrollmentID == dispute.EnrollmentID 
                                && r.RequestType == RefundRequestType.DisputeResolved);

                if (existingRefund)
                {
                    continue; // Đã xử lý rồi
                }

                // Lấy enrollment
                var enrollment = await unitOfWork.ClassEnrollments.GetByIdAsync(dispute.EnrollmentID);
                if (enrollment == null) continue;

                // Tính số tiền hoàn
                decimal refundAmount = dispute.Status == DisputeStatus.Resolved_Refunded
                    ? enrollment.AmountPaid
                    : enrollment.AmountPaid * 0.5m; // Partial = 50%

                // Tạo RefundRequest cho học viên
                var refundRequest = new RefundRequest
                {
                    RefundRequestID = Guid.NewGuid(),
                    EnrollmentID = enrollment.EnrollmentID,
                    ClassID = teacherClass.ClassID,
                    StudentID = dispute.StudentID,
                    RequestType = RefundRequestType.DisputeResolved,
                    Reason = $"Dispute được chấp nhận: {dispute.Reason}",
                    RefundAmount = refundAmount,
                    Status = RefundRequestStatus.Draft,
                    BankName = string.Empty,
                    BankAccountNumber = string.Empty,
                    BankAccountHolderName = string.Empty,
                    RequestedAt = now,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                await unitOfWork.RefundRequests.CreateAsync(refundRequest);

                // Cập nhật enrollment status
                enrollment.Status = DAL.Models.EnrollmentStatus.PendingRefund;
                enrollment.UpdatedAt = now;

                _logger.LogInformation(
                    "✅ [ClassLifecycle] Created RefundRequest {RefundId} for dispute {DisputeId}, Amount: {Amount}",
                    refundRequest.RefundRequestID, dispute.DisputeID, refundAmount);

                // Gửi thông báo cho học viên
                var student = await unitOfWork.Users.GetByIdAsync(dispute.StudentID);
                if (student != null && !string.IsNullOrEmpty(student.FcmToken))
                {
                    try
                    {
                        await firebaseService.SendNotificationAsync(
                            student.FcmToken,
                            "Khiếu nại được chấp nhận ✅",
                            $"Bạn sẽ được hoàn {refundAmount:N0} VND từ lớp '{teacherClass.Title}'. Vui lòng cập nhật thông tin ngân hàng để nhận tiền.",
                            new Dictionary<string, string>
                            {
                                { "type", "dispute_resolved_refund" },
                                { "disputeId", dispute.DisputeID.ToString() },
                                { "refundRequestId", refundRequest.RefundRequestID.ToString() },
                                { "amount", refundAmount.ToString() }
                            }
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "[FCM] Failed to send dispute result notification to student");
                    }
                }
            }
        }

        /// <summary>
        /// Xử lý payout cho giáo viên:
        /// - Tính tổng tiền từ enrollments (TRỪ các enrollment có dispute refund)
        /// - Trừ platform fee (10%)
        /// - Cộng vào ví giáo viên (90%)
        /// - Tạo transaction cho cả Admin và Teacher wallet
        /// - Chuyển trạng thái sang Completed_Paid
        /// </summary>
        private async Task ProcessTeacherPayouts(IUnitOfWork unitOfWork, IFirebaseNotificationService firebaseService, DateTime now)
        {
            try
            {
                var classesForPayout = await unitOfWork.TeacherClasses.Query()
                    .Include(c => c.Teacher)
                        .ThenInclude(t => t!.TeacherProfile)
                    .Include(c => c.Enrollments)
                    .Include(c => c.Disputes)
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
            // Lấy danh sách enrollment IDs có dispute được refund (để loại trừ)
            var refundedEnrollmentIds = teacherClass.Disputes?
                .Where(d => d.Status == DisputeStatus.Resolved_Refunded 
                         || d.Status == DisputeStatus.Resolved_PartialRefund)
                .Select(d => d.EnrollmentID)
                .ToHashSet() ?? new HashSet<Guid>();

            // 1. Tính tổng tiền từ các enrollment đã thanh toán (LOẠI TRỪ các dispute refund)
            var paidEnrollments = teacherClass.Enrollments?
                .Where(e => (e.Status == DAL.Models.EnrollmentStatus.Paid 
                          || e.Status == DAL.Models.EnrollmentStatus.Completed)
                         && !refundedEnrollmentIds.Contains(e.EnrollmentID))
                .ToList() ?? new List<ClassEnrollment>();

            // Tính tiền từ partial refund (chỉ lấy 50%)
            var partialRefundEnrollments = teacherClass.Enrollments?
                .Where(e => refundedEnrollmentIds.Contains(e.EnrollmentID))
                .Join(teacherClass.Disputes!.Where(d => d.Status == DisputeStatus.Resolved_PartialRefund),
                      e => e.EnrollmentID,
                      d => d.EnrollmentID,
                      (e, d) => e)
                .ToList() ?? new List<ClassEnrollment>();

            var totalRevenue = paidEnrollments.Sum(e => e.AmountPaid) 
                             + partialRefundEnrollments.Sum(e => e.AmountPaid * 0.5m); // 50% cho partial refund

            if (totalRevenue <= 0)
            {
                _logger.LogInformation(
                    "ℹ️ [ClassLifecycle] Class {ClassId} has no revenue after disputes - marking as Completed_Paid directly",
                    teacherClass.ClassID);

                teacherClass.Status = ClassStatus.Completed_Paid;
                teacherClass.UpdatedAt = now;
                await unitOfWork.SaveChangesAsync();
                return;
            }

            var platformFee = totalRevenue * PLATFORM_FEE_PERCENTAGE;
            var teacherPayout = totalRevenue * TEACHER_PERCENTAGE;

            _logger.LogInformation(
                "💵 [ClassLifecycle] Class {ClassId}: TotalRevenue={Total} (after disputes), PlatformFee={Fee} (10%), TeacherPayout={Payout} (90%)",
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

            // 3. Lấy ví Admin (Platform)
            var adminWallet = await unitOfWork.Wallets.Query()
                .FirstOrDefaultAsync(w => w.OwnerType == OwnerType.Admin);

            if (adminWallet == null)
            {
                _logger.LogError("❌ [ClassLifecycle] Admin wallet not found!");
                return;
            }

            // 4. Kiểm tra Admin HoldBalance có đủ tiền không (tiền học phí nằm trong Hold)
            if (adminWallet.HoldBalance < totalRevenue)
            {
                _logger.LogError(
                    "❌ [ClassLifecycle] Admin wallet insufficient HoldBalance! Hold: {Hold}, Required: {Required}",
                    adminWallet.HoldBalance, totalRevenue);
                return;
            }

            // 5. Chuyển tiền từ HoldBalance của Admin
            // - Trừ toàn bộ từ HoldBalance
            // - Platform fee (10%) vào AvailableBalance của Admin
            // - Teacher payout (90%) chuyển sang Teacher
            adminWallet.HoldBalance -= totalRevenue;
            adminWallet.AvailableBalance += platformFee; // Admin giữ 10%
            adminWallet.TotalBalance -= teacherPayout; // Trừ 90% khỏi Total
            adminWallet.UpdatedAt = now;

            // 6. Tạo transaction cho Admin (platform fee)
            var adminPlatformFeeTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = DAL.Type.TransactionType.Transfer,
                Amount = platformFee,
                ReferenceId = teacherClass.ClassID,
                ReferenceType = DAL.Type.ReferenceType.Class,
                Description = $"Platform fee (10%) từ lớp '{teacherClass.Title}'",
                Status = DAL.Type.TransactionStatus.Succeeded,
                CreatedAt = now
            };
            await unitOfWork.WalletTransactions.AddAsync(adminPlatformFeeTransaction);

            // 7. Tạo transaction cho Admin (chuyển cho Teacher)
            var adminPayoutTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = DAL.Type.TransactionType.Payout,
                Amount = -teacherPayout,
                ReferenceId = teacherClass.ClassID,
                ReferenceType = DAL.Type.ReferenceType.Class,
                Description = $"Chuyển tiền cho GV từ lớp '{teacherClass.Title}' ({paidEnrollments.Count + partialRefundEnrollments.Count} học viên)",
                Status = DAL.Type.TransactionStatus.Succeeded,
                CreatedAt = now
            };
            await unitOfWork.WalletTransactions.AddAsync(adminPayoutTransaction);

            // 8. Cộng tiền vào ví giáo viên
            teacherWallet.TotalBalance += teacherPayout;
            teacherWallet.AvailableBalance += teacherPayout;
            teacherWallet.UpdatedAt = now;

            // 9. Tạo wallet transaction cho giáo viên (cộng tiền)
            var teacherTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = teacherWallet.WalletId,
                TransactionType = DAL.Type.TransactionType.Payout,
                Amount = teacherPayout,
                ReferenceId = teacherClass.ClassID,
                ReferenceType = DAL.Type.ReferenceType.Class,
                Description = $"Nhận thanh toán lớp học '{teacherClass.Title}' ({paidEnrollments.Count + partialRefundEnrollments.Count} học viên, 90%)",
                Status = DAL.Type.TransactionStatus.Succeeded,
                CreatedAt = now
            };
            await unitOfWork.WalletTransactions.AddAsync(teacherTransaction);

            // 10. Tạo TeacherPayout record
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
                StaffId = Guid.Empty,
                CreatedAt = now,
                UpdatedAt = now
            };
            await unitOfWork.TeacherPayouts.CreateAsync(teacherPayoutRecord);

            // 11. Cập nhật trạng thái enrollments (chỉ các enrollment không bị refund)
            foreach (var enrollment in paidEnrollments)
            {
                enrollment.Status = DAL.Models.EnrollmentStatus.Completed;
                enrollment.UpdatedAt = now;
            }

            // 12. Cập nhật trạng thái lớp
            teacherClass.Status = ClassStatus.Completed_Paid;
            teacherClass.UpdatedAt = now;

            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation(
                "✅ [ClassLifecycle] Payout completed for class {ClassId}: {Amount:N0} VND transferred to Teacher wallet. Admin kept {Fee:N0} VND (10%)",
                teacherClass.ClassID, teacherPayout, platformFee);

            // 13. Gửi thông báo cho giáo viên
            if (teacherClass.Teacher != null && !string.IsNullOrEmpty(teacherClass.Teacher.FcmToken))
            {
                try
                {
                    var disputeNote = refundedEnrollmentIds.Any() 
                        ? $" (đã trừ {refundedEnrollmentIds.Count} dispute)" 
                        : "";

                    await firebaseService.SendNotificationAsync(
                        teacherClass.Teacher.FcmToken,
                        "Thanh toán lớp học thành công 💰",
                        $"Bạn đã nhận được {teacherPayout:N0} VND từ lớp '{teacherClass.Title}'{disputeNote}",
                        new Dictionary<string, string>
                        {
                            { "type", "class_payout_completed" },
                            { "classId", teacherClass.ClassID.ToString() },
                            { "amount", teacherPayout.ToString() },
                            { "walletTransactionId", teacherTransaction.WalletTransactionId.ToString() }
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
