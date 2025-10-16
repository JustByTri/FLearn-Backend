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
        public virtual LearnerLanguage Learner { get; set; }
        [Required]
        public Guid ExerciseId { get; set; }
        [ForeignKey(nameof(ExerciseId))]
        public virtual Exercise Exercise { get; set; }
        public string? AudioUrl { get; set; }
        public string? AudioPublicId { get; set; }
        public double? AIScore { get; set; } = 0.0;
        public string? AIFeedback { get; set; }
        public double? TeacherScore { get; set; } = 0.0;
        public string? TeacherFeedback { get; set; }
        public int? XPGranted { get; set; }
        public bool IsPassed { get; set; }
        public ExerciseSubmissionStatus Status { get; set; } = ExerciseSubmissionStatus.PendingAiReview;
        // Liên kết tới lần nộp cũ (nếu là sửa hoặc làm lại)
        public Guid? PreviousSubmissionId { get; set; }
        [ForeignKey(nameof(PreviousSubmissionId))]
        public virtual ExerciseSubmission? PreviousSubmission { get; set; }
        public int RevisionCount { get; set; } = 0; // số lần nộp lại
        public DateTime SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public ICollection<ExerciseEvaluationDetail>? EvaluationDetails { get; set; } = new List<ExerciseEvaluationDetail>(); // Chi tiết đánh giá
        public ICollection<Conversation>? Conversations { get; set; } = new List<Conversation>(); // Các cuộc hội thoại liên quan đến bài tập này
    }
}
