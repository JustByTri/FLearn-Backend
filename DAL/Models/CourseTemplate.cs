using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class CourseTemplate
    {
        [Key]
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public string? Description { get; set; }
        public bool RequireGoal { get; set; }
        public bool RequireLevel { get; set; }
        public bool RequireSkillFocus { get; set; }
        public bool RequireTopic { get; set; }
        public bool RequireLang { get; set; }
        public int MinUnits { get; set; }
        public int MinLessonsPerUnit { get; set; }
        public int MinExercisesPerLesson { get; set; }
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
