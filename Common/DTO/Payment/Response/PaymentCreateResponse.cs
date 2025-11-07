namespace Common.DTO.Payment.Response
{
    public class PaymentCreateResponse
    {
        public string PaymentUrl { get; set; } = null!;
        public string? TransactionReference { get; set; }
        public string? ExpiresAt { get; set; }
    }
}
