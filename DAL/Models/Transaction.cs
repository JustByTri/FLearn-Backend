using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Transaction
    {
        [Key]
        public Guid TransactionId { get; set; }
        public Guid? UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        public Guid? CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; }
        public Guid? PurchaseId { get; set; }
        [ForeignKey(nameof(PurchaseId))]
        public virtual Purchase? Purchases { get; set; }
        public double TotalAmount { get; set; }
        public string TransactionCode { get; set; }
        public TransactionStatus Status { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string? Note { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
    }
}
