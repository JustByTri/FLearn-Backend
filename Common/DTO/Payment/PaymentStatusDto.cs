using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Payment
{
    public class PaymentStatusDto
    {
        public string TransactionId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime? PaidAt { get; set; }
        public string? PaymentMethod { get; set; }
    }
}
