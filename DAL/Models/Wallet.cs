using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Wallet
    {
        [Key]
        public Guid WalletId { get; set; }
        public Guid? OwnerId { get; set; }
        [ForeignKey(nameof(OwnerId))]
        public virtual User? Owner { get; set; } = null!;
        public Guid? TeacherId { get; set; }

        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile? TeacherProfile { get; set; }
        [Required]
        public OwnerType OwnerType { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        public decimal TotalBalance { get; set; } = 0.0m;
        public decimal AvailableBalance { get; set; } = 0.0m;
        public decimal HoldBalance { get; set; } = 0.0m;
        public CurrencyType Currency { get; set; } = CurrencyType.VND;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new HashSet<WalletTransaction>();
    }
}
