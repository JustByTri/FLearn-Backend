using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class SlotPurchase
    {
        [Key]
        public Guid SlotPurchaseId { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; }
        public int SlotQuantity { get; set; } = 0;
        public double SlotPrice { get; set; }
        public double DiscountAmount { get; set; }
        public double TotalAmount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public PurchaseStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public virtual ICollection<Transaction>? Transactions { get; set; } = new List<Transaction>();
    }
}
