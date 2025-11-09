using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class ManagerLanguage
    {
        [Key]
        public Guid ManagerId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        public virtual User User { get; set; } = null!;
        [Required]
        public Guid LanguageId { get; set; }
        public virtual Language Language { get; set; } = null!;
        public bool Status { get; set; } = true;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public virtual ICollection<TeacherApplication> TeacherApplications { get; set; } = new List<TeacherApplication>();
        public virtual ICollection<CourseSubmission> CourseSubmissions { get; set; } = new List<CourseSubmission>();
        public virtual ICollection<ExerciseGradingAssignment> ExerciseGradingAssignments { get; set; } = new List<ExerciseGradingAssignment>();
    }
}
