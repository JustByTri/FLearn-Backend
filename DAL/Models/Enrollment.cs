using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Enrollment
    {
        [Key]
        public Guid EnrollmentID { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; }
        [Required]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; }
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
        [Range(0, 100)]
        public double ProgressPercent { get; set; } = 0;
        public DateTime EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public virtual ICollection<LearnerProgress> LessonProgresses { get; set; } = new List<LearnerProgress>();
        public virtual ICollection<LessonActivityLog>? LessonActivityLogs { get; set; } = new List<LessonActivityLog>();
    }
}
