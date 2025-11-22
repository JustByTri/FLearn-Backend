namespace Common.DTO.Refund.Response
{
    public class RefundRequestResponse
    {
        public Guid RefundRequestId { get; set; }
        public Guid PurchaseId { get; set; }
        public Guid StudentId { get; set; }
        public string StudentName { get; set; }
        public string StudentEmail { get; set; }
        public string? StudentAvatar { get; set; }
        public string CourseName { get; set; }
        public decimal RefundAmount { get; set; }
        public decimal OriginalAmount { get; set; }
        public string RequestType { get; set; }
        public string Reason { get; set; }
        public string BankName { get; set; }
        public string BankAccountNumber { get; set; }
        public string BankAccountHolderName { get; set; }
        public string? ProofImageUrl { get; set; }
        public string Status { get; set; }
        public string RequestedAt { get; set; }
        public string? ProcessedAt { get; set; }
        public string? AdminNote { get; set; }
        public string? ProcessedByAdminName { get; set; }
    }
}
