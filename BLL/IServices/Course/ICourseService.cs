using Common.DTO.ApiResponse;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Course
{
    public interface ICourseService
    {
        Task<BaseResponse<CourseResponse>> CreateCourseAsync(Guid teacherId, CourseRequest request);
        Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesAsync(PagingRequest request);
        Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesByTeacherIdAsync(Guid teacherId, PagingRequest request);
        Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(Guid courseId);
        Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid teacherId, Guid courseId, UpdateCourseRequest request);
    }
}
