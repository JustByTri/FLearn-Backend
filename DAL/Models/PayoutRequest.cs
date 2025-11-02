using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class PayoutRequest
    {
        [Key]
        public Guid PayoutRequestId { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile Teacher { get; set; } = null!;
        [Required]
        public Guid BankAccountId { get; set; }
        [ForeignKey(nameof(BankAccountId))]
        public virtual TeacherBankAccount BankAccount { get; set; } = null!;
        public Guid? ApprovedBy { get; set; }
        [ForeignKey(nameof(ApprovedBy))]
        public virtual User Admin { get; set; } = null!;
        public decimal Amount { get; set; }
        public PayoutStatus PayoutStatus { get; set; } = PayoutStatus.Pending;
        public DateTime RequestedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? ApprovedAt { get; set; }
        [StringLength(500)]
        public string TransactionRef { get; set; } = null!;
        [StringLength(250)]
        public string PayoutChannel { get; set; } = null!;
        public CurrencyType CurrencyType { get; set; } = CurrencyType.VND;
        [StringLength(500)]
        public string Note { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? UpdatedAt { get; set; }
    }
}
