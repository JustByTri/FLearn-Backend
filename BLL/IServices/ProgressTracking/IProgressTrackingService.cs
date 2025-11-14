using Common.DTO.ApiResponse;
using Common.DTO.ExerciseSubmission.Response;
using Common.DTO.Paging.Response;
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
        Task<PagedResponse<List<ExerciseSubmissionDetailResponse>>> GetMySubmissionsAsync(Guid userId, Guid? courseId, Guid? lessonId, string? status, int pageNumber, int pageSize);
        Task<BaseResponse<ExerciseSubmissionDetailResponse>> GetSubmissionDetailAsync(Guid userId, Guid submissionId);
        Task<PagedResponse<List<ExerciseSubmissionHistoryResponse>>> GetExerciseSubmissionsHistoryAsync(Guid userId, Guid exerciseId, int pageNumber, int pageSize);
    }
}
