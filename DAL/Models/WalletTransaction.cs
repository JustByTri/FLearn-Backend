using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class WalletTransaction
    {
        [Key]
        public Guid WalletTransactionId { get; set; }
        [Required]
        public Guid WalletId { get; set; }
        [ForeignKey(nameof(WalletId))]
        public virtual Wallet Wallet { get; set; } = null!;
        public TransactionType? TransactionType { get; set; }
        public decimal Amount { get; set; }
        public Guid? ReferenceId { get; set; }
        public ReferenceType? ReferenceType { get; set; }
        public string? Description { get; set; }
        public TransactionStatus? Status { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
