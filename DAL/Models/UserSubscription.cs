using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class UserSubscription
    {
        [Key]
        public Guid SubscriptionID { get; set; }
        [Required]
        public Guid UserID { get; set; }
        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; } = null!;
        [Required]
        [StringLength(50)]
        public string SubscriptionType { get; set; } = string.Empty; // Free, Basic5, Basic10, Basic15
        public int ConversationQuota { get; set; } // 5, 10, 15
        public DateTime StartDate { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? EndDate { get; set; } = TimeHelper.GetVietnamTime();
        public bool IsActive { get; set; } = true;
        public decimal Price { get; set; } = 0; // For tracking purchases
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
