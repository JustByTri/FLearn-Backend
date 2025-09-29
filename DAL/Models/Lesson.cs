using DAL.Type;
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
        public required string Title { get; set; }
        [StringLength(int.MaxValue)]
        public string? Content { get; set; }
        [Required]
        [Range(1, 10, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Position { get; set; }

        [Required(ErrorMessage = "Skill focus is required.")]
        public SkillType SkillFocus { get; set; }
        [StringLength(500, ErrorMessage = "Description cannot exceed {1} characters.")]
        public string? Description { get; set; }
        [Url(ErrorMessage = "Invalid video URL format")]
        public string? VideoUrl { get; set; }
        public string? VideoPublicId { get; set; }

        [Url(ErrorMessage = "Invalid document URL format")]
        public string? DocumentUrl { get; set; }
        public string? DocumentPublicId { get; set; }
        public int? TotalExercises { get; set; } = 0;
        [Required]
        public Guid CourseUnitID { get; set; }
        [ForeignKey(nameof(CourseUnitID))]
        public CourseUnit? CourseUnit { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Exercise> Exercises { get; set; } = new List<Exercise>();
    }
}
