using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class CourseSubmission
    {
        [Key]
        public Guid CourseSubmissionID { get; set; }
        [Required]
        public Guid CourseID { get; set; }
        [ForeignKey(nameof(CourseID))]
        public virtual Course Course { get; set; }
        [Required]
        public Guid SubmittedById { get; set; }
        [ForeignKey(nameof(SubmittedById))]
        public virtual TeacherProfile SubmittedBy { get; set; }
        [Required]
        public Guid ReviewedById { get; set; }
        [ForeignKey(nameof(ReviewedById))]
        public virtual StaffLanguage ReviewedBy { get; set; }
        public SubmissionStatus Status { get; set; } = SubmissionStatus.Pending;
        public string? Feedback { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
    }
}
