using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ExerciseGrading.Request
{
    public class TeacherGradingRequest
    {
        [Required]
        [Range(0, 100, ErrorMessage = "Score must be between 0 and 100")]
        public double Score { get; set; }
        [Required]
        [StringLength(1000, ErrorMessage = "Feedback cannot exceed 1000 characters")]
        public string Feedback { get; set; } = string.Empty;
    }
}
