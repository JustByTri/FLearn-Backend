using Common.DTO.ApiResponse;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Course
{
    public interface ICourseService
    {
        Task<BaseResponse<CourseResponse>> CreateCourseAsync(Guid userId, CourseRequest request);
        Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesAsync(PagingRequest request, string status);
        Task<PagedResponse<IEnumerable<CourseResponse>>> GetAllCoursesByTeacherIdAsync(Guid userId, PagingRequest request, string status);
        Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(Guid courseId);
        Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid userId, Guid courseId, UpdateCourseRequest request);
        Task<BaseResponse<object>> SubmitCourseForReviewAsync(Guid userId, Guid courseId);
        Task<BaseResponse<object>> ApproveCourseSubmissionAsync(Guid userId, Guid submissionId);
        Task<BaseResponse<object>> RejectCourseSubmissionAsync(Guid userId, Guid submissionId, string reason);
        Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetAllCourseSubmissionsByStaffAsync(Guid userId, PagingRequest request, string status);
        Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetAllCourseSubmissionsByTeacherAsync(Guid userId, PagingRequest request, string status);
    }
}
