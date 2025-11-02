using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class CourseTemplate
    {
        [Key]
        public Guid TemplateId { get; set; }
        [Required]
        public Guid ProgramId { get; set; }
        [ForeignKey(nameof(ProgramId))]
        public virtual Program Program { get; set; } = null!;
        [Required]
        public Guid LevelId { get; set; }
        [ForeignKey(nameof(LevelId))]
        public virtual Level Level { get; set; } = null!;
        public string Name { get; set; } = null!;
        public string Description { get; set; } = null!;
        public int UnitCount { get; set; }
        public int LessonsPerUnit { get; set; }
        public int ExercisesPerLesson { get; set; }
        public string ScoringCriteriaJson { get; set; } = null!;
        public bool Status { get; set; } = true;
        public string Version { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime? ModifiedAt { get; set; }
        public virtual ICollection<Course> Courses { get; set; } = new List<Course>();
    }
}
