using Common.DTO.ApiResponse;
using Common.DTO.LessonProgress.Response;

namespace BLL.IServices.LessonProgress
{
    public interface ILessonProgressService
    {
        Task<BaseResponse<LearnerProgressResponse>> StartLessonAsync(Guid userId, Guid enrollmentId, Guid lessonId);
        Task<BaseResponse<LearnerProgressResponse>> UpdateLessonProgressAsync(Guid userId, Guid enrollmentId, Guid lessonId, double progressPercent);
        Task<BaseResponse<LearnerProgressResponse>> CompleteLessonAsync(Guid userId, Guid enrollmentId, Guid lessonId, bool forceComplete = false);
        Task<BaseResponse<LearnerProgressResponse>> GetLessonProgressAsync(Guid userId, Guid enrollmentId, Guid lessonId);
        Task<BaseResponse<IEnumerable<LearnerProgressResponse>>> GetProgressesByEnrollmentAsync(Guid userId, Guid enrollmentId);
        Task<double> RecalculateEnrollmentProgressAsync(Guid enrollmentId);
    }
}
