using DAL.Helpers;
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
        [Required]
        public Guid CourseId { get; set; }
        public virtual Course Course { get; set; } = null!;
        [Required]
        public decimal TotalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public PurchaseStatus Status { get; set; } = PurchaseStatus.Pending;
        public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.PayOS;
        public CurrencyType CurrencyType { get; set; } = CurrencyType.VND;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? PaidAt { get; set; }
        public virtual Enrollment? Enrollment { get; set; }
    }
}
