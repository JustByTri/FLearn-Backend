using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class PaymentTransaction
    {
        [Key]
        public Guid TransactionId { get; set; }
        [Required]
        public Guid PurchaseId { get; set; }
        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase Purchase { get; set; } = null!;
        [Required]
        public decimal Amount { get; set; }
        [StringLength(100)]
        public string TransactionRef { get; set; } = null!;
        [Required]
        public TransactionStatus TransactionStatus { get; set; } = TransactionStatus.Pending;
        [Required]
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.PayOS;
        [Required]
        public CurrencyType CurrencyType { get; set; } = CurrencyType.VND;
        public bool Status { get; set; } = true;
        [StringLength(500)]
        public string? GatewayResponse { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? CompletedAt { get; set; }
    }
}
