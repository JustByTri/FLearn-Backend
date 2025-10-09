using Common.DTO.ApiResponse;
using Common.DTO.Lesson.Request;
using Common.DTO.Lesson.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Lesson
{
    public interface ILessonService
    {
        Task<BaseResponse<LessonResponse>> CreateLessonAsync(Guid userId, Guid unitId, LessonRequest request);
        Task<BaseResponse<LessonResponse>> UpdateLessonAsync(Guid userId, Guid unitId, Guid lessonId, LessonUpdateRequest request);
        Task<PagedResponse<IEnumerable<LessonResponse>>> GetLessonsByUnitIdAsync(Guid unitId, PagingRequest request);
        Task<BaseResponse<LessonResponse>> GetLessonByIdAsync(Guid lessonId);
    }
}
