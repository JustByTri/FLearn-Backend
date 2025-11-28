using Common.DTO.ApiResponse;
using Common.DTO.LessonProgress.Response;

namespace BLL.IServices.ProgressTracking
{
    public interface ILessonProgressService
    {
        Task<BaseResponse<LessonProgressDetailResponse>> GetLessonProgressAsync(Guid userId, Guid lessonId);
        Task<BaseResponse<List<LessonProgressSummaryResponse>>> GetUnitLessonsProgressAsync(Guid userId, Guid unitId);
        Task<BaseResponse<LessonActivityStatusResponse>> GetLessonActivityStatusAsync(Guid userId, Guid lessonId);
        Task<BaseResponse<List<LessonExerciseProgressResponse>>> GetLessonExercisesWithStatusAsync(Guid userId, Guid lessonId);
    }
}
