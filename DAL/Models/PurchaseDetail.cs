using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class PurchaseDetail
    {
        [Key]
        public Guid PurchasesDetailID { get; set; }

        [Required]
        public Guid PurchaseId { get; set; }
        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase Purchase { get; set; }

        [Required]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; }
        [Required]
        public decimal PriceAtPurchase { get; set; }
        public decimal? DiscountPriceAtPurchase { get; set; }
        public bool IsRefunded { get; set; } = false;
        public DateTime CreatedAt { get; set; }
    }
}
