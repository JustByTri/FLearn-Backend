using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class StaffLanguage
    {
        [Key]
        public Guid StaffLanguageId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<Course>? ApprovedCourses { get; set; } = new List<Course>();
        public virtual ICollection<TeacherApplication>? TeacherApplications { get; set; } = new List<TeacherApplication>();
        public virtual ICollection<CourseSubmission>? CourseSubmissions { get; set; } = new List<CourseSubmission>();
        public virtual ICollection<LessonReview>? LessonReviews { get; set; } = new List<LessonReview>();
        public virtual ICollection<LessonDispute>? LessonDisputes { get; set; } = new List<LessonDispute>();
        public virtual ICollection<TeacherPayout>? TeacherPayouts { get; set; } = new List<TeacherPayout>();
    }
}
