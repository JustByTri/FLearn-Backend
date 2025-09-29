using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Exercise.Request
{
    public class ExerciseOptionRequest
    {
        [Required(ErrorMessage = "Option text is required.")]
        [StringLength(500, ErrorMessage = "Option text must not exceed 500 characters.")]
        public string Text { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
    }
}
