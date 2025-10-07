using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Purchase
    {
        [Key]
        public Guid PurchasesID { get; set; }

        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        [Required]
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();
        public virtual ICollection<Transaction>? Transactions { get; set; } = new List<Transaction>();
    }
}
