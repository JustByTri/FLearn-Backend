using Common.DTO.ApiResponse;
using Common.DTO.LessonLog.Request;
using Common.DTO.LessonLog.Response;

namespace BLL.IServices.LessonActivityLog
{
    public interface ILessonActivityLogService
    {
        Task<BaseResponse<LessonLogResponse>> AddLogAsync(Guid userId, LessonLogRequest req);
        Task<double> CalculateLessonProgressFromLogsAsync(Guid userId, Guid lessonId, Guid enrollmentId);
    }
}
