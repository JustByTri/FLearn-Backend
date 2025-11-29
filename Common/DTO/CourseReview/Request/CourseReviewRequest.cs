using System.ComponentModel.DataAnnotations;

namespace Common.DTO.CourseReview.Request
{
    public class CourseReviewRequest
    {
        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }
        [Required]
        [StringLength(1000)]
        public string? Comment { get; set; }
    }
}
