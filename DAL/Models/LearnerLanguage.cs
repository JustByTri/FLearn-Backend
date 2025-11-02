using DAL.Helpers;
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
        public virtual User User { get; set; } = null!;
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; } = null!;
        public string ProficiencyLevel { get; set; } = string.Empty;
        public int StreakDays { get; set; } = 0;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public virtual ICollection<ExerciseSubmission> ExerciseSubmissions { get; set; } = new List<ExerciseSubmission>();
        public virtual ICollection<ConversationSession> ConversationSessions { get; set; } = new List<ConversationSession>();
        public virtual ICollection<LearnerAchievement> Achievements { get; set; } = new List<LearnerAchievement>();
        public virtual ICollection<TeacherReview> TeacherReviews { get; set; } = new List<TeacherReview>();
        public virtual ICollection<CourseReview> CourseReviews { get; set; } = new List<CourseReview>();
        public virtual ICollection<LessonActivityLog> LessonActivityLogs { get; set; } = new List<LessonActivityLog>();
    }
}
