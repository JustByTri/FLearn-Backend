using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ExerciseSubmission
    {
        [Key]
        public Guid ExerciseSubmissionId { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; } = null!;
        [Required]
        public Guid ExerciseId { get; set; }
        [ForeignKey(nameof(ExerciseId))]
        public virtual Exercise Exercise { get; set; } = null!;
        [Required]
        public Guid LessonProgressId { get; set; }
        [ForeignKey(nameof(LessonProgressId))]
        public virtual LessonProgress LessonProgress { get; set; } = null!;
        [Url(ErrorMessage = "Invalid audio URL format")]
        public string AudioUrl { get; set; } = null!;
        public string AudioPublicId { get; set; } = null!;
        public double AIScore { get; set; } = 0.0;
        public string AIFeedback { get; set; } = null!;
        public double TeacherScore { get; set; } = 0.0;
        public string TeacherFeedback { get; set; } = null!;
        public double? FinalScore { get; set; }
        [Range(0, 100)]
        public double AIPercentage { get; set; } = 50;
        [Range(0, 100)]
        public double? TeacherPercentage { get; set; } = 50;
        public bool? IsPassed { get; set; }
        public ExerciseSubmissionStatus Status { get; set; } = ExerciseSubmissionStatus.PendingAIReview;
        public DateTime SubmittedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? ReviewedAt { get; set; }
        public virtual ICollection<ExerciseGradingAssignment> ExerciseGradingAssignments { get; set; } = new List<ExerciseGradingAssignment>();
    }
}
