using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Goal
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }
        [Required]
        [MaxLength(100)]
        public required string Name { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool Status { get; set; } = true; // Active by default or false for inactive
        public virtual ICollection<CourseGoal>? CourseGoals { get; set; } = new List<CourseGoal>();
        public virtual ICollection<LearnerGoal>? LearnerGoals { get; set; } = new List<LearnerGoal>();
    }
}
