using DAL.Helpers;
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
        public virtual LearnerLanguage Learner { get; set; } = null!;
        [Required]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; } = null!;
        public DAL.Type.EnrollmentStatus Status { get; set; } = DAL.Type.EnrollmentStatus.Active;
        [Range(0, 100)]
        public double ProgressPercent { get; set; } = 0;
        public DateTime? LastAccessedAt { get; set; }
        public int TotalTimeSpent { get; set; } = 0;
        public Guid? CurrentUnitId { get; set; }
        public Guid? CurrentLessonId { get; set; }
        public int CompletedUnits { get; set; } = 0;
        public int TotalUnits { get; set; } = 0;
        public int CompletedLessons { get; set; } = 0;
        public int TotalLessons { get; set; } = 0;
        public DateTime? LastLessonCompletedAt { get; set; }
        public DateTime EnrolledAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? CompletedAt { get; set; }
        public virtual ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
        public virtual ICollection<UnitProgress> UnitProgresses { get; set; } = new List<UnitProgress>();
    }
}
