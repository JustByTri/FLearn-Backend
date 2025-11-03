using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Purchase
    {
        [Key]
        public Guid PurchasesId { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        // Course purchase (optional for subscription purchases)
        public Guid? CourseId { get; set; }
        public virtual Course? Course { get; set; }

        // Subscription purchase (optional for course purchases)
        public Guid? SubscriptionId { get; set; }
        [ForeignKey(nameof(SubscriptionId))]
        public virtual UserSubscription? Subscription { get; set; }

        [Required]
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.PayOS;
        public CurrencyType CurrencyType { get; set; } = CurrencyType.VND;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }

        // Only for course purchases
        public virtual Enrollment? Enrollment { get; set; }
    }
}
