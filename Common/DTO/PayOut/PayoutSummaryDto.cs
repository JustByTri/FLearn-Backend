using System;
namespace Common.DTO.PayOut
{
    public class PayoutSummaryDto
    {
        public Guid PayoutRequestId { get; set; }
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
    }
}
