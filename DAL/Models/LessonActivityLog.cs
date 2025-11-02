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
        public Lesson Lesson { get; set; } = null!;
        [Required]
        public Guid LessonProgressId { get; set; }
        [ForeignKey(nameof(LessonProgressId))]
        public LessonProgress LessonProgress { get; set; } = null!;
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public LearnerLanguage Learner { get; set; } = null!;
        public LessonLogType ActivityType { get; set; }
        public double? Value { get; set; }
        public string? MetadataJson { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
