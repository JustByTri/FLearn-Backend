using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class ExerciseGradingAssignment
    {
        [Key]
        public Guid GradingAssignmentId { get; set; }
        [Required]
        public Guid ExerciseSubmissionId { get; set; }
        [ForeignKey(nameof(ExerciseSubmissionId))]
        public virtual ExerciseSubmission ExerciseSubmission { get; set; } = null!;
        public Guid? AssignedTeacherId { get; set; }
        [ForeignKey(nameof(AssignedTeacherId))]
        public virtual TeacherProfile Teacher { get; set; } = null!;
        public DateTime AssignedAt { get; set; } = TimeHelper.GetVietnamTime();
        [Required]
        public DateTime DeadlineAt { get; set; }
        [Required]
        public GradingStatus Status { get; set; } = GradingStatus.Pending;
        public DateTime? StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Feedback { get; set; }
        public double? FinalScore { get; set; }
        public DateTime? RevokedAt { get; set; }
        public Guid? RevokedBy { get; set; }
        [ForeignKey(nameof(RevokedBy))]
        public virtual ManagerLanguage? Manager { get; set; }
        public string? RevokeReason { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual TeacherEarningAllocation? EarningAllocation { get; set; }
    }
}
