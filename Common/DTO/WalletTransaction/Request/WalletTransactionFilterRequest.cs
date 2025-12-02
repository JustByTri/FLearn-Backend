using Common.DTO.Paging.Request;
using DAL.Type;

namespace Common.DTO.WalletTransaction.Request
{
    public class WalletTransactionFilterRequest : PaginationParams
    {
        public TransactionType? TransactionType { get; set; }
        public TransactionStatus? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }
}
