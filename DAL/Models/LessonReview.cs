using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class LessonReview
    {
        [Key]
        public Guid LessonReviewId { get; set; }
        [Required]
        public Guid LessonBookingId { get; set; }
        [ForeignKey(nameof(LessonBookingId))]
        public virtual LessonBooking LessonBooking { get; set; }
        public Guid? StaffId { get; set; } // UserId + LanguageId 
        [ForeignKey(nameof(StaffId))]
        public virtual StaffLanguage Staff { get; set; }
        public bool IsQualified { get; set; } = false;
        public string? Note { get; set; }
        public DateTime ReviewedAt { get; set; }
    }
}
