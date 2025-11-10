using Microsoft.AspNetCore.Http;

namespace Common.DTO.ExerciseGrading.Request
{
    public class AssessmentRequest
    {
        public Guid ExerciseSubmissionId { get; set; }
        public IFormFile Audio { get; set; } = null!;
        public string GradingType { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
    }
}
