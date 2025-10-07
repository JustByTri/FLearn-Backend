using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ContentIssueReport
    {
        [Key]
        public Guid ReportId { get; set; }
        [Required]
        public Guid LearnerId { get; set; } // UserId + LanguageId
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; }
        public Guid? LessonId { get; set; }
        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
        public Guid? ExerciseId { get; set; }
        [ForeignKey(nameof(ExerciseId))]
        public virtual Exercise? Exercise { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ReportStatus Status { get; set; } = ReportStatus.Pending;
        public string? LearnerEvidenceUrl { get; set; }
        public DateTime SubmittedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
    }
}
