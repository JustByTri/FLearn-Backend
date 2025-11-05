
using DAL.Type;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
namespace Common.DTO.Exercise.Request
{
    public class ExerciseRequest
    {
        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, ErrorMessage = "Title must not exceed 200 characters.")]
        public string Title { get; set; } = string.Empty;
        [StringLength(1000, ErrorMessage = "Prompt must not exceed 1000 characters.")]
        public string? Prompt { get; set; }
        [StringLength(500, ErrorMessage = "Hints must not exceed 500 characters.")]
        public string? Hints { get; set; }
        [StringLength(2000, ErrorMessage = "Content must not exceed 2000 characters.")]
        public string? Content { get; set; }
        [StringLength(1000, ErrorMessage = "Expected answer must not exceed 1000 characters.")]
        public string? ExpectedAnswer { get; set; }
        public List<IFormFile>? MediaFiles { get; set; }
        [Required(ErrorMessage = "Exercise type is required.")]
        public SpeakingExerciseType Type { get; set; }
        [Required(ErrorMessage = "Difficulty level is required.")]
        public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Easy;
        [Required(ErrorMessage = "Max score is required.")]
        [Range(1, 100, ErrorMessage = "Max score must be between {1} and {2}.")]
        public int MaxScore { get; set; }
        [Required(ErrorMessage = "Pass score is required.")]
        [Range(1, 100, ErrorMessage = "Pass score must be between {1} and {2}.")]
        public int PassScore { get; set; }
        [StringLength(1000, ErrorMessage = "Feedback for correct answer must not exceed 1000 characters.")]
        public string? FeedbackCorrect { get; set; }
        [StringLength(1000, ErrorMessage = "Feedback for incorrect answer must not exceed 1000 characters.")]
        public string? FeedbackIncorrect { get; set; }
    }
}
