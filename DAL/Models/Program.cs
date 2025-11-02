using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Program
    {
        [Key]
        public Guid ProgramId { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; } = null!;
        [Required]
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public bool Status { get; set; } = true; // Active by default
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<Level> Levels { get; set; } = new List<Level>(); // Levels associated with this program
        public virtual ICollection<CourseTemplate> CourseTemplates { get; set; } = new List<CourseTemplate>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<TeacherProgramAssignment> TeacherProgramAssignments { get; set; } = new List<TeacherProgramAssignment>();
    }
}
