using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using Common.DTO.Paging.Response;

namespace BLL.IServices.ProgressTracking
{
    public interface IExerciseGradingService
    {
        Task<BaseResponse<bool>> ProcessAIGradingAsync(AssessmentRequest request);
        Task<BaseResponse<bool>> ProcessTeacherGradingAsync(Guid exerciseSubmissionId, Guid userId, double score, string feedback);
        Task<BaseResponse<ExerciseGradingStatusResponse>> GetGradingStatusAsync(Guid exerciseSubmissionId);
        Task<PagedResponse<List<ExerciseGradingAssignmentResponse>>> GetTeacherAssignmentsAsync(Guid userId, GradingAssignmentFilterRequest filter);
        Task<BaseResponse<bool>> AssignExerciseToTeacherAsync(Guid exerciseSubmissionId, Guid userId, Guid teacherId);
        Task<BaseResponse<bool>> CheckAndReassignExpiredAssignmentsAsync();
        Task<PagedResponse<List<ExerciseGradingAssignmentResponse>>> GetManagerAssignmentsAsync(Guid userId, GradingAssignmentFilterRequest filter);
        Task<BaseResponse<GradingFilterOptionsResponse>> GetGradingFilterOptionsAsync(Guid userId);
        Task<PagedResponse<List<EligibleTeacherResponse>>> GetEligibleTeachersForReassignmentAsync(EligibleTeacherFilterRequest filter);
    }
}
