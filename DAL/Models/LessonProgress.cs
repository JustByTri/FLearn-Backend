using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LessonProgress
    {
        [Key]
        public Guid LessonProgressId { get; set; }
        [Required]
        public Guid UnitProgressId { get; set; }
        public virtual UnitProgress? UnitProgress { get; set; }
        [Required]
        public Guid LessonId { get; set; }
        [ForeignKey(nameof(LessonId))]
        public virtual Lesson? Lesson { get; set; }
        public double ProgressPercent { get; set; } = 0.0;
        [Required]
        public LearningStatus Status { get; set; } = LearningStatus.NotStarted;
        public int CurrentExerciseIndex { get; set; } = 0;
        public bool? IsContentViewed { get; set; }
        public bool? IsVideoWatched { get; set; }
        public bool? IsDocumentRead { get; set; }
        public bool? IsPracticeCompleted { get; set; }
        public DateTime StartedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? CompletedAt { get; set; }
        public DateTime? LastUpdated { get; set; }
        public virtual ICollection<ExerciseSubmission> ExerciseSubmissions { get; set; } = new List<ExerciseSubmission>();
        public virtual ICollection<LessonActivityLog> LessonActivityLogs { get; set; } = new List<LessonActivityLog>();
    }
}
