using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class TeacherProfile
    {
        [Key]
        public Guid TeacherProfileId { get; set; }
        [Required]
        public Guid UserId { get; set; }
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; }
        [Required]
        public Guid LanguageId { get; set; }
        [ForeignKey(nameof(LanguageId))]
        public virtual Language Language { get; set; }
        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;
        [Required]
        public DateTime BirthDate { get; set; }
        [Required]
        [StringLength(500)]
        public string Bio { get; set; } = string.Empty;
        [Required]
        [StringLength(500)]
        public string Avatar { get; set; } = string.Empty;
        [Required]
        [EmailAddress]
        [StringLength(200)]
        public string Email { get; set; } = string.Empty;
        [Required]
        [Phone]
        [StringLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        public double AverageRating { get; set; } = 0.0;
        public int ReviewCount { get; set; } = 0;
        [Required]
        [StringLength(500)]
        public string MeetingUrl { get; set; } = string.Empty;
        public bool Status { get; set; } = true; // Active by default or false for inactive
        public bool? IsOpenToTeach { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public virtual ICollection<TeacherReview>? TeacherReviews { get; set; } = new List<TeacherReview>();
        public virtual ICollection<CourseSubmission>? CourseSubmissions { get; set; } = new List<CourseSubmission>();
        public virtual ICollection<Course>? Courses { get; set; } = new List<Course>();
        public virtual ICollection<LessonBooking>? LessonBookings { get; set; } = new List<LessonBooking>();
        public virtual ICollection<LessonDispute>? LessonDisputes { get; set; } = new List<LessonDispute>();
        public virtual ICollection<TeacherPayout>? TeacherPayouts { get; set; } = new List<TeacherPayout>();
    }
}
