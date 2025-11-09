using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;

namespace BLL.IServices.ProgressTracking
{
    public interface IExerciseGradingService
    {
        Task<BaseResponse<bool>> ProcessAIGradingAsync(AssessmentRequest request);
        Task<BaseResponse<bool>> ProcessTeacherGradingAsync(Guid exerciseSubmissionId, Guid userId, double score, string feedback);
        Task<BaseResponse<bool>> CheckAndReassignExpiredAssignmentsAsync();
        Task<BaseResponse<ExerciseGradingStatusResponse>> GetGradingStatusAsync(Guid exerciseSubmissionId);
        Task<BaseResponse<List<ExerciseGradingAssignmentResponse>>> GetTeacherAssignmentsAsync(Guid userId, GradingAssignmentFilterRequest filter);
    }
}
