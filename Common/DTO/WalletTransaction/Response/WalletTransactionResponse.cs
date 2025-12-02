namespace Common.DTO.WalletTransaction.Response
{
    public class WalletTransactionResponse
    {
        public Guid WalletTransactionId { get; set; }
        public decimal Amount { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }
}
