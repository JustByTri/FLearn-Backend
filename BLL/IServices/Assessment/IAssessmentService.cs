using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;

namespace BLL.IServices.Assessment
{
    public interface IAssessmentService
    {
        Task<AssessmentResult> EvaluateSpeakingAsync(AssessmentRequest req, CancellationToken ct = default);
    }
}
