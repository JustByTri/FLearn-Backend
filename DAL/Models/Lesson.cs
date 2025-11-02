using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Lesson
    {
        [Key]
        public Guid LessonID { get; set; }
        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;
        [StringLength(int.MaxValue)]
        public string Content { get; set; } = null!;
        [Required]
        [Range(1, 10, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Position { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed {1} characters.")]
        public string Description { get; set; } = null!;
        [Url(ErrorMessage = "Invalid video URL format")]
        public string VideoUrl { get; set; } = null!;
        public string VideoPublicId { get; set; } = null!;

        [Url(ErrorMessage = "Invalid document URL format")]
        public string DocumentUrl { get; set; } = null!;
        public string DocumentPublicId { get; set; } = null!;
        public int TotalExercises { get; set; } = 0;
        [Required]
        public Guid CourseUnitID { get; set; }
        [ForeignKey(nameof(CourseUnitID))]
        public virtual CourseUnit? CourseUnit { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
        public virtual ICollection<LessonProgress> LessonProgresses { get; set; } = new List<LessonProgress>();
        public virtual ICollection<LessonActivityLog> LessonActivityLogs { get; set; } = new List<LessonActivityLog>();
    }
}
