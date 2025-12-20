using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Subscription
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int SubscriptionId { get; set; }
        [Required]
        public string Name { get; set; } = null!;
        [Required]
        public decimal Price { get; set; }
        [Required]
        public int ConversationQuota { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
