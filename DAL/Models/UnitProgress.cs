using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class UnitProgress
    {
        [Key]
        public Guid UnitProgressId { get; set; }
        [Required]
        public Guid EnrollmentId { get; set; }
        [ForeignKey(nameof(EnrollmentId))]
        public virtual Enrollment Enrollment { get; set; } = null!;
        [Required]
        public Guid CourseUnitId { get; set; }
        [ForeignKey(nameof(CourseUnitId))]
        public virtual CourseUnit CourseUnit { get; set; } = null!;
        public double ProgressPercent { get; set; } = 0.0;
        [Required]
        public LearningStatus Status { get; set; } = LearningStatus.NotStarted;
        public DateTime StartedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
    }
}
