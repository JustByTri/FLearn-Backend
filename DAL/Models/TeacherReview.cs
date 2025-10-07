using System.ComponentModel.DataAnnotations;

namespace DAL.Models
{
    // Students evaluate teachers
    public class TeacherReview
    {
        [Key]
        public Guid TeacherReviewId { get; set; }
        [Required]
        public Guid TeacherProfileId { get; set; }
        public virtual TeacherProfile TeacherProfile { get; set; }
        [Required]
        public Guid LearnerId { get; set; } // UserId + LanguageId
        public virtual LearnerLanguage Learner { get; set; }
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        [StringLength(1000)]
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
