using System.ComponentModel.DataAnnotations;

namespace Common.DTO.LessonProgress.Request
{
    public class UpdateLessonProgressRequest
    {
        [Required]
        public Guid EnrollmentId { get; set; }

        [Range(0, 100)]
        public double ProgressPercent { get; set; }
    }
}
