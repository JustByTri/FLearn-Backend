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
        [StringLength(200, ErrorMessage = "Title must not exceed 200 characters.")]
        public required string Title { get; set; }
        [StringLength(1000, ErrorMessage = "Prompt must not exceed 1000 characters.")]
        public string? Prompt { get; set; }
        [StringLength(500, ErrorMessage = "Hints must not exceed 500 characters.")]
        public string? Hints { get; set; }
        [StringLength(2000, ErrorMessage = "Content must not exceed 2000 characters.")]
        public string? Content { get; set; }
        [StringLength(1000, ErrorMessage = "Expected answer must not exceed 1000 characters.")]
        public string? ExpectedAnswer { get; set; }
        // Media (audio/video/image/pdf/docx…)
        public string? MediaUrl { get; set; }
        public string? MediaPublicId { get; set; }
        [Range(1, 100, ErrorMessage = "Position must be between {1} and {2}.")]
        public int Position { get; set; }
        [Required]
        public SpeakingExerciseType Type { get; set; }
        [Required]
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;
        public int MaxScore { get; set; }
        public int PassScore { get; set; }
        [StringLength(1000)]
        public string? FeedbackCorrect { get; set; }
        [StringLength(1000)]
        public string? FeedbackIncorrect { get; set; }
        // Điều kiện tiên quyết (1 bài tập phải pass trước)
        public Guid? PrerequisiteExerciseID { get; set; }
        [ForeignKey(nameof(PrerequisiteExerciseID))]
        public virtual Exercise? PrerequisiteExercise { get; set; }
        [Required]
        public Guid LessonID { get; set; }
        [ForeignKey(nameof(LessonID))]
        public virtual Lesson? Lesson { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<ExerciseSubmission>? ExerciseSubmissions { get; set; } = new List<ExerciseSubmission>();
        public virtual ICollection<ContentIssueReport>? ContentIssueReports { get; set; } = new List<ContentIssueReport>();
    }
}
