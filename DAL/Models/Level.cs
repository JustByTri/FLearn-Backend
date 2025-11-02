using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class Level
    {
        [Key]
        public Guid LevelId { get; set; }
        [Required]
        public Guid ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))]
        public virtual Program Program { get; set; }
        [Required]
        public string Name { get; set; } // A1, A2, B1, B2, C1, C2, N5, N4, N3, N2, N1, etc.
        public int OrderIndex { get; set; } // Order of the level within the program; example: A1=1, A2=2, B1=3, etc.
        public string? Description { get; set; }
        public bool Status { get; set; } = true; // Active by default
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<CourseTemplate> CourseTemplates { get; set; } = new List<CourseTemplate>();
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
        public virtual ICollection<TeacherProgramAssignment> TeacherProgramAssignments { get; set; } = new List<TeacherProgramAssignment>();
    }
}
