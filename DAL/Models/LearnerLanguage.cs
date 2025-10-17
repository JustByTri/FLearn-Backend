using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LearnerLanguage
    {
        [Key]
        public Guid LearnerLanguageId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
        public string ProficiencyLevel { get; set; } = string.Empty;
        public int TotalXP { get; set; } = 0;
        public int StreakDays { get; set; } = 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<Roadmap>? Roadmaps { get; set; } = new List<Roadmap>();
        public virtual ICollection<TeacherReview>? TeacherReviews { get; set; } = new List<TeacherReview>();
        public virtual ICollection<CourseReview>? CourseReviews { get; set; } = new List<CourseReview>();
        public virtual ICollection<Enrollment>? Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<LearnerAchievement>? Achievements { get; set; } = new List<LearnerAchievement>();
        public virtual ICollection<ExerciseSubmission>? ExerciseSubmissions { get; set; } = new List<ExerciseSubmission>();
        public virtual ICollection<ContentIssueReport>? ContentIssueReports { get; set; } = new List<ContentIssueReport>();
        public virtual ICollection<LessonBooking>? LessonBookings { get; set; } = new List<LessonBooking>();
        public virtual ICollection<LearnerGoal>? Goals { get; set; } = new List<LearnerGoal>();
        public virtual ICollection<LessonActivityLog>? LessonActivityLogs { get; set; } = new List<LessonActivityLog>();
    }
}
