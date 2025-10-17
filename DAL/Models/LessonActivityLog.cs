using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LessonActivityLog
    {
        [Key]
        public Guid LessonActivityLogId { get; set; }
        [Required]
        public Guid LessonId { get; set; }
        [ForeignKey(nameof(LessonId))]
        public Lesson Lesson { get; set; }
        [Required]
        public Guid EnrollmentId { get; set; }   // which enrollment this log is for
        [ForeignKey(nameof(EnrollmentId))]
        public Enrollment Enrollment { get; set; }
        [Required]
        public Guid LearnerId { get; set; }         // learner
        [ForeignKey(nameof(LearnerId))]
        public LearnerLanguage Learner { get; set; }
        public LessonLogType ActivityType { get; set; }
        public double? Value { get; set; }       // percent/time/score as needed
        public string? MetadataJson { get; set; } // optional free-form JSON
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
