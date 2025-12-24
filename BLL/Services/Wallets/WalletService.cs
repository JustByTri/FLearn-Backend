using BLL.IServices.Wallets;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Wallets
{
    public class WalletService : IWalletService
    {
        private readonly IUnitOfWork _unitOfWork;
        private const decimal SYSTEM_FEE_PERCENTAGE = 0.10m;
        private const decimal TEACHER_FEE_PERCENTAGE = 0.90m;
        private const decimal COURSE_FEE_PERCENTAGE = 0.55m;
        private const decimal GRADING_FEE_PERCENTAGE = 0.35m;
        public WalletService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task TransferToAdminWalletAsync(Guid purchaseId)
        {
            var purchase = await _unitOfWork.Purchases.Query()
                .OrderBy(p => p.CreatedAt)
                .Include(p => p.Course)
                .FirstOrDefaultAsync(p => p.PurchasesId == purchaseId);

            if (purchase == null) return;

            var adminWallet = await GetOrCreateAdminWalletAsync();

            // 1. Tính toán các khoản tiền dựa trên tỷ lệ phần trăm
            decimal systemShare = purchase.FinalAmount * SYSTEM_FEE_PERCENTAGE;      // 10%
            decimal courseCreationShare = purchase.FinalAmount * COURSE_FEE_PERCENTAGE; // 55%
            decimal gradingShare = purchase.FinalAmount * GRADING_FEE_PERCENTAGE;    // 35%

            // 2. Cập nhật số dư ví Admin
            // Tổng tiền vẫn cộng đủ 100%
            adminWallet.TotalBalance += purchase.FinalAmount;

            // Tiền hệ thống vào Available
            adminWallet.AvailableBalance += systemShare;

            // Tiền Teacher (Tạo khóa + Chấm bài) vào Hold
            adminWallet.HoldBalance += (courseCreationShare + gradingShare);
            adminWallet.TotalBalance = adminWallet.AvailableBalance + adminWallet.HoldBalance;
            adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

            // 3. Tạo 3 Transaction riêng biệt để dễ dàng phân biệt

            // Transaction 1: Phí hệ thống (10%) - Status: Succeeded (Tiền vào túi ngay)
            var systemTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = systemShare,
                ReferenceId = purchase.PurchasesId,
                ReferenceType = ReferenceType.CoursePurchase, // Hoặc dùng loại riêng nếu có
                Description = $"Phí hệ thống (10%) - Thanh toán cho khóa học: {purchase.Course?.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            // Transaction 2: Phí tạo khóa học (55%) - Status: Succeeded (Nhưng nằm trong Hold Balance)
            var courseCreationTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = courseCreationShare,
                ReferenceId = purchase.PurchasesId,
                ReferenceType = ReferenceType.CourseCreationFee, // Dễ dàng filter sau này
                Description = $"Tạm giữ: Phí Tạo Khóa Học (55%) - Thanh toán cho: {purchase.Course?.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            // Transaction 3: Phí chấm bài (35%) - Status: Succeeded (Nhưng nằm trong Hold Balance)
            var gradingTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = gradingShare,
                ReferenceId = purchase.PurchasesId,
                ReferenceType = ReferenceType.GradingFee, // Dễ dàng filter sau này
                Description = $"Tạm giữ: Phí Chấm Điểm (35%) - Thanh toán cho: {purchase.Course?.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.WalletTransactions.CreateAsync(systemTransaction);
            await _unitOfWork.WalletTransactions.CreateAsync(courseCreationTransaction);
            await _unitOfWork.WalletTransactions.CreateAsync(gradingTransaction);

            await _unitOfWork.SaveChangesAsync();
        }
        public async Task TransferToTeacherWalletAsync(Guid purchaseId)
        {
            var purchase = await _unitOfWork.Purchases.GetByIdAsync(purchaseId);

            if (!purchase.CourseId.HasValue) return;

            var course = await _unitOfWork.Courses.Query()
                .Include(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .OrderByDescending(c => c.CreatedAt)
                .Where(c => c.CourseID == purchase.CourseId).FirstOrDefaultAsync();

            if (course == null) return;

            var teacherShare = purchase.FinalAmount * COURSE_FEE_PERCENTAGE;
            var adminWallet = await _unitOfWork.Wallets
                .FindAllAsync(w => w.OwnerType == OwnerType.Admin && w.Currency == CurrencyType.VND);

            if (!adminWallet.Any()) return;
            var adminWalletObj = adminWallet.First();
            adminWalletObj.HoldBalance -= teacherShare;
            adminWalletObj.TotalBalance = adminWalletObj.AvailableBalance + adminWalletObj.HoldBalance;
            adminWalletObj.UpdatedAt = TimeHelper.GetVietnamTime();

            var adminDebitTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWalletObj.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = teacherShare,
                ReferenceId = purchase.PurchasesId,
                ReferenceType = ReferenceType.TeacherPayout,
                Description = $"Transfer to teacher: {course.Teacher?.User?.FullName} - {course.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.WalletTransactions.CreateAsync(adminDebitTransaction);
            var teacherWallet = await _unitOfWork.Wallets.FindAllAsync(w => w.TeacherId == course.TeacherId && w.Currency == CurrencyType.VND);

            Wallet teacherWalletObj;

            if (!teacherWallet.Any())
            {
                teacherWalletObj = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    TeacherId = course.TeacherId,
                    OwnerType = OwnerType.Teacher,
                    Name = $"Ví giáo viên - {course.Teacher?.User?.FullName}",
                    Currency = CurrencyType.VND,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _unitOfWork.Wallets.CreateAsync(teacherWalletObj);
            }
            else
            {
                teacherWalletObj = teacherWallet.First();
            }

            teacherWalletObj.TotalBalance += teacherShare;
            teacherWalletObj.AvailableBalance += teacherShare;
            teacherWalletObj.UpdatedAt = TimeHelper.GetVietnamTime();

            var teacherCreditTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = teacherWalletObj.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = teacherShare,
                ReferenceId = purchase.PurchasesId,
                ReferenceType = ReferenceType.TeacherPayout,
                Description = $"Earnings from courses: {course.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.WalletTransactions.CreateAsync(teacherCreditTransaction);
        }
        public async Task ProcessCourseCreationFeeTransferAsync(Guid purchaseId)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var purchase = await _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(p => p.PurchasesId == purchaseId);

                if (purchase == null || purchase.Status != PurchaseStatus.Completed) return;

                var hasActiveRefundRequest = await _unitOfWork.RefundRequests.Query().AnyAsync(r => r.PurchaseId == purchaseId &&
                          (r.Status == RefundRequestStatus.Approved ||
                           r.Status == RefundRequestStatus.Pending));

                if (hasActiveRefundRequest)
                {
                    Console.WriteLine($"Purchase {purchaseId} has Pending/Approved refund. Skipping automatic transfer.");
                    return;
                }

                var adminWallet = await GetOrCreateAdminWalletAsync();

                decimal courseCreationAmount = purchase.FinalAmount * COURSE_FEE_PERCENTAGE;

                // Lưu ý: Admin HoldBalance giảm, Admin Available tăng (bước trung gian), sau đó chuyển sang Teacher
                // Chuyển tiền từ hold balance sang available balance của admin
                adminWallet.HoldBalance -= courseCreationAmount;
                adminWallet.AvailableBalance += courseCreationAmount;
                adminWallet.TotalBalance = adminWallet.AvailableBalance + adminWallet.HoldBalance;
                adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

                var adminTransaction = new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = adminWallet.WalletId,
                    TransactionType = TransactionType.Transfer,
                    Amount = courseCreationAmount,
                    ReferenceId = purchaseId,
                    ReferenceType = ReferenceType.CourseCreationFee,
                    Description = $"Phí tạo khóa học được giải phóng sau thời gian hoàn tiền: {purchase.Course?.Title}",
                    Status = TransactionStatus.Succeeded,
                    CreatedAt = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.WalletTransactions.CreateAsync(adminTransaction);
                await _unitOfWork.SaveChangesAsync();

                // Chuyển tiền tạo khóa học cho teacher
                await TransferCourseCreationFeeToTeacherAsync(purchaseId, courseCreationAmount);
            });
        }
        private async Task TransferCourseCreationFeeToTeacherAsync(Guid purchaseId, decimal amount)
        {
            var purchase = await _unitOfWork.Purchases.Query()
                .Include(p => p.Course)
                    .ThenInclude(c => c.Teacher)
                    .ThenInclude(t => t.User)
                .FirstOrDefaultAsync(p => p.PurchasesId == purchaseId);

            if (purchase?.Course?.TeacherId == null) return;

            var adminWallet = await GetOrCreateAdminWalletAsync();
            var teacherWallet = await GetOrCreateTeacherWalletAsync(purchase.Course.TeacherId);

            adminWallet.AvailableBalance -= amount;
            adminWallet.TotalBalance = adminWallet.AvailableBalance + adminWallet.HoldBalance;
            adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

            var adminDebitTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = TransactionType.Payout,
                Amount = -amount,
                ReferenceId = purchaseId,
                ReferenceType = ReferenceType.TeacherPayout,
                Description = $"Phí tạo khóa học cho giáo viên: {purchase.Course.Teacher?.FullName} - {purchase.Course.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            teacherWallet.TotalBalance += amount;
            teacherWallet.AvailableBalance += amount;
            teacherWallet.UpdatedAt = TimeHelper.GetVietnamTime();

            var teacherCreditTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = teacherWallet.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = amount,
                ReferenceId = purchaseId,
                ReferenceType = ReferenceType.TeacherPayout,
                Description = $"Phí tạo khóa học: {purchase.Course.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.WalletTransactions.CreateAsync(adminDebitTransaction);
            await _unitOfWork.WalletTransactions.CreateAsync(teacherCreditTransaction);
            await _unitOfWork.SaveChangesAsync();
        }
        public async Task TransferExerciseGradingFeeToTeacherAsync(Guid allocationId)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var allocation = await _unitOfWork.TeacherEarningAllocations.Query()
                    .Include(a => a.GradingAssignment)
                    .Include(a => a.Teacher)
                        .ThenInclude(t => t.User)
                    .FirstOrDefaultAsync(a => a.AllocationId == allocationId);

                if (allocation == null ||
                    allocation.Status != EarningStatus.Approved ||
                    allocation.ExerciseGradingAmount == null ||
                    allocation.ExerciseGradingAmount <= 0)
                    return;

                var adminWallet = await GetOrCreateAdminWalletAsync();
                var teacherWallet = await GetOrCreateTeacherWalletAsync(allocation.TeacherId);
                decimal amount = allocation.ExerciseGradingAmount.Value;

                var existingTransfer = await _unitOfWork.WalletTransactions.Query()
                    .AnyAsync(wt => wt.ReferenceId == allocationId &&
                                   wt.ReferenceType == ReferenceType.GradingFee);

                if (existingTransfer) return;

                adminWallet.HoldBalance -= amount;
                adminWallet.TotalBalance = adminWallet.AvailableBalance + adminWallet.HoldBalance;
                adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

                var adminDebitTransaction = new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = adminWallet.WalletId,
                    TransactionType = TransactionType.Payout,
                    Amount = -amount,
                    ReferenceId = allocationId,
                    ReferenceType = ReferenceType.GradingFee,
                    Description = $"Phí chấm điểm cho giáo viên: {allocation.Teacher?.FullName}",
                    Status = TransactionStatus.Succeeded,
                    CreatedAt = TimeHelper.GetVietnamTime()
                };

                teacherWallet.TotalBalance += amount;
                teacherWallet.AvailableBalance += amount;
                teacherWallet.UpdatedAt = TimeHelper.GetVietnamTime();

                var teacherCreditTransaction = new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = teacherWallet.WalletId,
                    TransactionType = TransactionType.Transfer,
                    Amount = amount,
                    ReferenceId = allocationId,
                    ReferenceType = ReferenceType.GradingFee,
                    Description = $"Phí chấm điểm cho giáo viên: {allocation.Teacher?.FullName}",
                    Status = TransactionStatus.Succeeded,
                    CreatedAt = TimeHelper.GetVietnamTime()
                };

                await _unitOfWork.WalletTransactions.CreateAsync(adminDebitTransaction);
                await _unitOfWork.WalletTransactions.CreateAsync(teacherCreditTransaction);

                allocation.Status = EarningStatus.Approved;
                allocation.UpdatedAt = TimeHelper.GetVietnamTime();

                await _unitOfWork.SaveChangesAsync();
            });
        }
        #region Private Methods
        private async Task<Wallet> GetOrCreateTeacherWalletAsync(Guid teacherId)
        {
            var teacherWallet = await _unitOfWork.Wallets
                .FindAllAsync(w => w.TeacherId == teacherId && w.Currency == CurrencyType.VND);

            if (!teacherWallet.Any())
            {
                var teacher = await _unitOfWork.TeacherProfiles.GetByIdAsync(teacherId);
                var wallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    TeacherId = teacherId,
                    OwnerType = OwnerType.Teacher,
                    Name = $"Teacher wallet - {teacher?.FullName ?? "Unknown"}",
                    Currency = CurrencyType.VND,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.Wallets.CreateAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
                return wallet;
            }

            return teacherWallet.First();
        }
        private async Task<Wallet> GetOrCreateAdminWalletAsync()
        {
            var adminWallet = await _unitOfWork.Wallets
                .FindAllAsync(w => w.OwnerType == OwnerType.Admin && w.Currency == CurrencyType.VND);

            if (!adminWallet.Any())
            {
                var wallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    OwnerType = OwnerType.Admin,
                    Name = "System administration wallet",
                    Currency = CurrencyType.VND,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.Wallets.CreateAsync(wallet);
                await _unitOfWork.SaveChangesAsync();
                return wallet;
            }
            return adminWallet.First();
        }
        #endregion
    }
}
