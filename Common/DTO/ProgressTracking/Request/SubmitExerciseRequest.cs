using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ProgressTracking.Request
{
    public class SubmitExerciseRequest
    {
        [Required]
        public Guid ExerciseId { get; set; }
        [Required]
        public IFormFile Audio { get; set; } = null!;
    }
}
