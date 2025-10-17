using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Common.DTO.ExerciseSubmission.Request
{
    public class ExerciseSubmissionRequest
    {
        [Required]
        public Guid ExerciseId { get; set; }
        [Required]
        public IFormFile Audio { get; set; }

    }
}
