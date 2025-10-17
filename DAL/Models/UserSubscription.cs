using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Models
{
    public class UserSubscription
    {
        [Key]
        public Guid SubscriptionID { get; set; }

        [Required]
        public Guid UserID { get; set; }

        [ForeignKey(nameof(UserID))]
        public virtual User User { get; set; }

        [Required]
        [StringLength(50)]
        public string SubscriptionType { get; set; } // Free, Basic5, Basic10, Basic15

        public int ConversationQuota { get; set; } // 5, 10, 15

        public DateTime StartDate { get; set; } = DateTime.UtcNow;
        public DateTime? EndDate { get; set; }

        public bool IsActive { get; set; } = true;

        public decimal Price { get; set; } = 0; // For tracking purchases

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
