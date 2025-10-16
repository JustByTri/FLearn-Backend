using System.ComponentModel.DataAnnotations;

namespace Common.DTO.LessonProgress.Request
{
    public class StartLessonRequest
    {
        [Required]
        public Guid EnrollmentId { get; set; }
    }
}
