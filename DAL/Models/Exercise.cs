using DAL.Helpers;
using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Exercise
    {
        [Key]
        public Guid ExerciseID { get; set; }
        [Required]
        public Guid LessonID { get; set; }
        [ForeignKey(nameof(LessonID))]
        public virtual Lesson? Lesson { get; set; }
        [Required]
        [StringLength(200, ErrorMessage = "Title must not exceed 200 characters.")]
        public string Title { get; set; } = null!;
        [StringLength(1000, ErrorMessage = "Prompt must not exceed 1000 characters.")]
        public string Prompt { get; set; } = null!;
        [StringLength(500, ErrorMessage = "Hints must not exceed 500 characters.")]
        public string Hints { get; set; } = null!;
        [StringLength(2000, ErrorMessage = "Content must not exceed 2000 characters.")]
        public string Content { get; set; } = null!;
        [StringLength(1000, ErrorMessage = "Expected answer must not exceed 1000 characters.")]
        public string ExpectedAnswer { get; set; } = null!;
        [StringLength(1000)]
        // Media (audio/video/image/pdf/docx…)
        public string MediaUrl { get; set; } = null!;
        [StringLength(1000)]
        public string MediaPublicId { get; set; } = null!;
        [Range(1, 100, ErrorMessage = "Position must be between {1} and {2}.")]
        public int Position { get; set; } = 1;
        [Required]
        public SpeakingExerciseType Type { get; set; } = SpeakingExerciseType.RepeatAfterMe;
        [Required]
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;
        [Required]
        [Range(0, 100, ErrorMessage = "Max score must be between {1} and {2}.")]
        public int MaxScore { get; set; } = 100;
        [Required]
        [Range(0, 100, ErrorMessage = "Max score must be between {1} and {2}.")]
        public int PassScore { get; set; } = 50;
        [StringLength(1000)]
        public string FeedbackCorrect { get; set; } = null!;
        [StringLength(1000)]
        public string FeedbackIncorrect { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<ExerciseSubmission> ExerciseSubmissions { get; set; } = new List<ExerciseSubmission>();
    }
}
