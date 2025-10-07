using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LearnerProgress
    {
        [Key]
        public Guid LearnerProgressId { get; set; }
        [Required]
        public Guid LearnerId { get; set; } // UserId + LanguageId
        public virtual LearnerLanguage Learner { get; set; }
        [Required]
        public Guid LessonId { get; set; }
        [ForeignKey(nameof(LessonId))]
        public virtual Lesson Lesson { get; set; }
        public bool IsCompleted { get; set; } = false;
        [Range(0, 100)]
        public double ProgressPercent { get; set; } = 0;
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
    }
}
