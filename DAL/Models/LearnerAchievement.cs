using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LearnerAchievement
    {
        [Key]
        public Guid LearnerAchievementId { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; } = null!;
        [Required]
        public Guid AchievementId { get; set; }
        [ForeignKey(nameof(AchievementId))]
        public virtual Achievement Achievement { get; set; } = null!;
        public DateTime? AchievedAt { get; set; }
    }
}
