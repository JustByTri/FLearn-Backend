using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Enrollment.Request
{
    public class EnrollmentRequest
    {
        [Required(ErrorMessage = "CourseId is required.")]
        public Guid CourseId { get; set; }
    }
}
