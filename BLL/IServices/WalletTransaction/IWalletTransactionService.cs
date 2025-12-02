using Common.DTO.Paging.Response;
using Common.DTO.WalletTransaction.Request;
using Common.DTO.WalletTransaction.Response;

namespace BLL.IServices.WalletTransactions
{
    public interface IWalletTransactionService
    {
        Task<PagedResponse<List<WalletTransactionResponse>>> GetWalletTransactionsAsync(Guid userId, WalletTransactionFilterRequest request);
    }
}
