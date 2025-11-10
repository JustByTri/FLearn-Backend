using System.ComponentModel.DataAnnotations;

namespace Common.DTO.PayOut
{
    public class ProcessPayoutRequestDto
    {
        [Required(ErrorMessage = "Quy?t ??nh x? lý là b?t bu?c")]
        public string Action { get; set; } = string.Empty; // "approve" ho?c "reject"

        [StringLength(500)]
        public string? AdminNote { get; set; }

        [StringLength(100)]
        public string? TransactionReference { get; set; } // Mã giao d?ch chuy?n kho?n th?c t?
    }

    public class PayoutRequestDetailDto
    {
        public Guid PayoutRequestId { get; set; }
        public Guid TeacherId { get; set; }
        public string TeacherName { get; set; } = string.Empty;
        public string TeacherEmail { get; set; } = string.Empty;
        public Guid BankAccountId { get; set; }
        public string BankName { get; set; } = string.Empty;
        public string BankBranch { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountHolder { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PayoutStatus { get; set; } = string.Empty;
        public DateTime RequestedAt { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public string? TransactionRef { get; set; }
        public string? Note { get; set; }
        public string? AdminNote { get; set; }
    }
}
