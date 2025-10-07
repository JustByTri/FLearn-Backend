using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class LearnerSlotBalance
    {
        [Key]
        public Guid LearnerSlotBalanceId { get; set; }
        [Required]
        public Guid LearnerId { get; set; } // UserId + LanguageId
        public virtual LearnerLanguage Learner { get; set; }
        public int TotalSlots { get; set; } = 0;
        public int UsedSlots { get; set; } = 0;
        public int RemainingSlots { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
