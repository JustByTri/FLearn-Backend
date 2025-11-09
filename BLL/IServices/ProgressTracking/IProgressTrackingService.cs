using Common.DTO.ApiResponse;
using Common.DTO.ProgressTracking.Request;
using Common.DTO.ProgressTracking.Response;

namespace BLL.IServices.ProgressTracking
{
    public interface IProgressTrackingService
    {
        Task<BaseResponse<ProgressTrackingResponse>> StartLessonAsync(Guid userId, StartLessonRequest request);
        Task<BaseResponse<ProgressTrackingResponse>> TrackActivityAsync(Guid userId, TrackActivityRequest request);
        Task<BaseResponse<ExerciseSubmissionResponse>> SubmitExerciseAsync(Guid userId, SubmitExerciseRequest request);
        Task<BaseResponse<ProgressTrackingResponse>> GetCurrentProgressAsync(Guid userId, Guid courseId);
    }
}
