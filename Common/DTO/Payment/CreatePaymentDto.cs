using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Payment
{
    public class CreatePaymentDto
    {
        [Required]
        public Guid ClassID { get; set; }

        [Required]
        public Guid StudentID { get; set; }

        [Required]
        public decimal Amount { get; set; }

        public string? ReturnUrl { get; set; }
        public string? CancelUrl { get; set; }
        public string Description { get; set; } = string.Empty;

        // Additional fields for PayOS
        public string ItemName { get; set; } = string.Empty;
        public string BuyerName { get; set; } = string.Empty;
        public string BuyerEmail { get; set; } = string.Empty;
        public string BuyerPhone { get; set; } = string.Empty;
    }
}
