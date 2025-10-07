using DAL.Type;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LessonDispute
    {
        [Key]
        public Guid LessonDisputeId { get; set; }
        [Required]
        public Guid LessonBookingId { get; set; }
        [ForeignKey(nameof(LessonBookingId))]
        public virtual LessonBooking LessonBooking { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
        public virtual TeacherProfile Teacher { get; set; }
        public Guid? StaffId { get; set; }
        public virtual StaffLanguage? Staff { get; set; }
        public string? Reason { get; set; }
        public string? EvidenceUrl { get; set; }
        public string? ReviewNote { get; set; }
        public DisputeStatus? Status { get; set; } = DisputeStatus.Submitted;
        public DateTime SubmittedAt { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}
