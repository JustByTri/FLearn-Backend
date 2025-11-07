using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Wallets
{
    public class TeacherPayoutJobService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly WalletService _walletService;
        private readonly ILogger<TeacherPayoutJobService> _logger;

        public TeacherPayoutJobService(
            IUnitOfWork unitOfWork,
            WalletService walletService,
            ILogger<TeacherPayoutJobService> logger)
        {
            _unitOfWork = unitOfWork;
            _walletService = walletService;
            _logger = logger;
        }

        public async Task ProcessTeacherPayoutAsync(Guid purchaseId)
        {
            await _unitOfWork.BeginTransactionAsync();

            try
            {
                var purchase = await _unitOfWork.Purchases.GetByIdAsync(purchaseId);
                if (purchase == null || purchase.Status != PurchaseStatus.Completed)
                {
                    return;
                }

                var refundRequests = await _unitOfWork.RefundRequests
                    .FindAllAsync(rr =>
                        rr.PurchaseId == purchaseId &&
                        rr.CreatedAt >= purchase.PaidAt &&
                        rr.CreatedAt <= purchase.PaidAt.Value.AddDays(3)
                    );

                if (!refundRequests.Any())
                {
                    await _walletService.TransferToTeacherWalletAsync(purchase.PurchasesId);
                }

                await _unitOfWork.SaveChangesAsync();
                await _unitOfWork.CommitTransactionAsync();
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync();
                _logger.LogError(ex, "Error when payout teacher: {Message}", ex.Message);
            }
        }
    }
}
