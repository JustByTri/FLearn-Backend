using Common.DTO.ApiResponse;
using Common.DTO.Course;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Course
{
    public interface ICourseService
    {
        Task<BaseResponse<CourseResponse>> CreateCourseAsync(Guid userId, CourseRequest request);
        Task<BaseResponse<CourseResponse>> GetCourseByIdAsync(Guid courseId);
        Task<PagedResponse<IEnumerable<CourseResponse>>> GetCoursesByTeacherAsync(Guid userId, PagingRequest request, string status);
        Task<PagedResponse<IEnumerable<CourseResponse>>> GetCoursesAsync(PagingRequest request, string status, string lang);
        Task<BaseResponse<CourseResponse>> UpdateCourseAsync(Guid userId, Guid courseId, UpdateCourseRequest request);
        Task<bool> DeleteCourseAsync(Guid courseId);
        Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetCourseSubmissionsByManagerAsync(Guid userId, PagingRequest request, string status);
        Task<PagedResponse<IEnumerable<CourseSubmissionResponse>>> GetCourseSubmissionsByTeacherAsync(Guid userId, PagingRequest request, string status);
        Task<BaseResponse<object>> SubmitCourseForReviewAsync(Guid userId, Guid courseId);
        Task<BaseResponse<object>> ApproveCourseSubmissionAsync(Guid userId, Guid submissionId);
        Task<BaseResponse<object>> RejectCourseSubmissionAsync(Guid userId, Guid submissionId, string reason);
        Task<BaseResponse<IEnumerable<PopularCourseDto>>> GetPopularCoursesAsync(int count = 10);
    }

}
