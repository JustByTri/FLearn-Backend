using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class CourseUnit
    {
        [Key]
        public Guid CourseUnitID { get; set; }
        [Required]
        [StringLength(200)]
        public required string Title { get; set; }
        [StringLength(500)]
        public string? Description { get; set; }
        [Range(1, 100, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public int Position { get; set; } = 1;
        [Required]
        public Guid CourseID { get; set; }
        [ForeignKey(nameof(CourseID))]
        public Course? Course { get; set; }
        public int? TotalLessons { get; set; } = 0;
        public bool? IsPreview { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    }
}
