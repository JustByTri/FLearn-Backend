using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Payment
{
    public class PaymentResponseDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string PaymentUrl { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ExpiryAt { get; set; }

        // Additional fields for success/error handling
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public DateTime ExpiryTime { get; set; }
    }
}
