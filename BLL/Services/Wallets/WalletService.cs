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
            var purchase = await _unitOfWork.Purchases.GetByIdAsync(purchaseId);

            var adminWallet = await _unitOfWork.Wallets
                .FindAllAsync(w => w.OwnerType == OwnerType.Admin && w.Currency == CurrencyType.VND);

            Wallet wallet;

            if (!adminWallet.Any())
            {
                wallet = new Wallet
                {
                    WalletId = Guid.NewGuid(),
                    OwnerType = OwnerType.Admin,
                    Name = "System administration wallet",
                    Currency = CurrencyType.VND,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };
                await _unitOfWork.Wallets.CreateAsync(wallet);
            }
            else
            {
                wallet = adminWallet.First();
            }

            wallet.TotalBalance += purchase.FinalAmount;
            wallet.AvailableBalance += purchase.FinalAmount * SYSTEM_FEE_PERCENTAGE;
            wallet.HoldBalance += purchase.FinalAmount * TEACHER_FEE_PERCENTAGE;
            wallet.UpdatedAt = TimeHelper.GetVietnamTime();

            var adminWalletTransaction = new WalletTransaction
            {
                WalletTransactionId = Guid.NewGuid(),
                WalletId = wallet.WalletId,
                TransactionType = TransactionType.Transfer,
                Amount = purchase.FinalAmount,
                ReferenceId = purchase.PurchasesId,
                Description = $"Revenue from course purchases: {purchase.Course?.Title}",
                Status = TransactionStatus.Succeeded,
                CreatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.WalletTransactions.CreateAsync(adminWalletTransaction);
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
    }
}
