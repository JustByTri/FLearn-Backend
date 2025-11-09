using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherEarningAllocation
    {
        [Key]
        public Guid AllocationId { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
        [ForeignKey(nameof(TeacherId))]
        public virtual TeacherProfile? Teacher { get; set; }
        [Required]
        public Guid GradingAssignmentId { get; set; }
        [ForeignKey(nameof(GradingAssignmentId))]
        public virtual ExerciseGradingAssignment? GradingAssignment { get; set; }
        public Guid? ApprovedBy { get; set; }
        [ForeignKey(nameof(ApprovedBy))]
        public virtual ManagerLanguage? Manager { get; set; }
        public decimal? CourseCreationAmount { get; set; }
        public decimal? ExerciseGradingAmount { get; set; }
        [Required]
        public EarningType EarningType { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public EarningStatus Status { get; set; } = EarningStatus.Pending;
        public DateTime CreatedAt { get; set; } = DAL.Helpers.TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = DAL.Helpers.TimeHelper.GetVietnamTime();
    }
}
