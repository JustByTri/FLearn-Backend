using System;
namespace Common.DTO.PayOut
{
    public class PayoutRequestDto
    {
        public Guid PayoutRequestId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? Note { get; set; }
        public string? BankName { get; set; }
        public string? AccountNumber { get; set; }
    }
}
