using DAL.Helpers;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DAL.Models
{
    public class CourseReview
    {
        [Key]
        public Guid CourseReviewId { get; set; }
        [Required]
        public Guid LearnerId { get; set; }
        [ForeignKey(nameof(LearnerId))]
        public virtual LearnerLanguage Learner { get; set; } = null!;
        [Required]
        public Guid CourseId { get; set; }
        [ForeignKey(nameof(CourseId))]
        public virtual Course Course { get; set; } = null!;
        [Required]
        [Range(0, 5)]
        public int Rating { get; set; }
        [Required]
        [StringLength(500)]
        public string Comment { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = TimeHelper.GetVietnamTime();
        public DateTime ModifiedDate { get; set; } = TimeHelper.GetVietnamTime();
    }
}
