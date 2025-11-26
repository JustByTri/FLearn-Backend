using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ExerciseGrading.Request
{
    public class AssignExerciseRequest
    {
        [Required]
        public Guid ExerciseSubmissionId { get; set; }
        [Required]
        public Guid TeacherId { get; set; }
    }
}
