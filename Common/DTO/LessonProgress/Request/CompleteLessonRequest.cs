using System.ComponentModel.DataAnnotations;

namespace Common.DTO.LessonProgress.Request
{
    public class CompleteLessonRequest
    {
        [Required]
        public Guid EnrollmentId { get; set; }
        // If true, mark lesson as completed regardless of percent (useful for marking after teacher review)
        public bool ForceComplete { get; set; } = false;
    }
}
