using DAL.Helpers;
using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    public class TeacherReview
    {
        [Key]
        public Guid TeacherReviewId { get; set; }
        [Required]
        public Guid TeacherProfileId { get; set; }
        public virtual TeacherProfile TeacherProfile { get; set; } = null!;
        [Required]
        public Guid LearnerId { get; set; }
        public virtual LearnerLanguage Learner { get; set; } = null!;
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        [StringLength(1000)]
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime UpdatedAt { get; set; } = TimeHelper.GetVietnamTime();
    }
}
