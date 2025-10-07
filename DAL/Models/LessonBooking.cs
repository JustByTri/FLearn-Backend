using DAL.Type;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class LessonBooking
    {
        [Key]
        public Guid LessonBookingId { get; set; }
        [Required]
        public Guid LearnerId { get; set; } // UserId + LanguageId
        public virtual LearnerLanguage Learner { get; set; }
        [Required]
        public Guid TeacherId { get; set; } // UserId + LanguageId
        public virtual TeacherProfile Teacher { get; set; }
        [Required]
        public DateTime StartDate { get; set; }
        [Required]
        public DateTime EndDate { get; set; }
        public string? RecordedUrl { get; set; }
        public LessonBookingStatus Status { get; set; } = LessonBookingStatus.Pending;
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public DateTime? ReviewedAt { get; set; }
        public virtual LessonReview? Reviews { get; set; }
        public virtual LessonDispute? Disputes { get; set; }
    }
}
