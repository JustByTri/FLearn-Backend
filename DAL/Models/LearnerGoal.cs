using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LearnerGoal
    {
        [Key]
        public Guid LearnerGoalId { get; set; }
        [Required]
        public int GoalId { get; set; }
        [ForeignKey(nameof(GoalId))]
        public Goal Goal { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public LearnerLanguage Learner { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
