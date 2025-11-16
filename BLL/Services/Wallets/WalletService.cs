using BLL.IServices.Wallets;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Hangfire;
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
            var purchase = await _unitOfWork.Purchases.GetByIdAsync(purchaseId);
            if (purchase == null) return;

            var adminWallet = await GetOrCreateAdminWalletAsync();

            decimal teacherShare = purchase.FinalAmount * TEACHER_FEE_PERCENTAGE;
            decimal systemShare = purchase.FinalAmount * SYSTEM_FEE_PERCENTAGE;

            adminWallet.TotalBalance += purchase.FinalAmount;
            adminWallet.AvailableBalance += systemShare;
            adminWallet.HoldBalance += teacherShare;
            adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

            var adminWalletTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = purchase.FinalAmount,
                ReferenceId = purchase.PurchasesId,
                ReferenceType = ReferenceType.CoursePurchase,
                Description = $"Revenue from course purchase: {purchase.Course?.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.WalletTransactions.CreateAsync(adminWalletTransaction);
            await _unitOfWork.SaveChangesAsync();

            // Schedule refund check after 3 days
            BackgroundJob.Schedule(() => ProcessCourseCreationFeeTransferAsync(purchaseId),
                TimeSpan.FromDays(3));
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

                // Kiểm tra xem đã có refund chưa
                var hasRefund = await _unitOfWork.WalletTransactions.Query()
                    .AnyAsync(wt => wt.ReferenceId == purchaseId &&
                                   wt.ReferenceType == ReferenceType.Refund);

                if (hasRefund) return;

                var adminWallet = await GetOrCreateAdminWalletAsync();
                decimal courseCreationAmount = purchase.FinalAmount * TEACHER_FEE_PERCENTAGE * COURSE_FEE_PERCENTAGE;

                // Chuyển tiền từ hold balance sang available balance của admin
                adminWallet.HoldBalance -= courseCreationAmount;
                adminWallet.AvailableBalance += courseCreationAmount;
                adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

                var adminTransaction = new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = adminWallet.WalletId,
                    TransactionType = TransactionType.Transfer,
                    Amount = courseCreationAmount,
                    ReferenceId = purchaseId,
                    ReferenceType = ReferenceType.CourseCreationFee,
                    Description = $"Course creation fee released after refund period: {purchase.Course?.Title}",
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
            adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

            var adminDebitTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = adminWallet.WalletId,
                TransactionType = TransactionType.Payout,
                Amount = -amount,
                ReferenceId = purchaseId,
                ReferenceType = ReferenceType.TeacherPayout,
                Description = $"Course creation fee to teacher: {purchase.Course.Teacher?.FullName} - {purchase.Course.Title}",
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
                Description = $"Course creation fee: {purchase.Course.Title}",
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
                adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

                var adminDebitTransaction = new WalletTransaction
                {
                    WalletTransactionId = Guid.NewGuid(),
                    WalletId = adminWallet.WalletId,
                    TransactionType = TransactionType.Payout,
                    Amount = -amount,
                    ReferenceId = allocationId,
                    ReferenceType = ReferenceType.GradingFee,
                    Description = $"Exercise grading fee to teacher: {allocation.Teacher?.FullName}",
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
                    Description = $"Exercise grading fee",
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
