using Common.DTO.ApiResponse;
using Common.DTO.Enrollment.Request;
using Common.DTO.Enrollment.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Enrollment
{
    public interface IEnrollmentService
    {
        Task<BaseResponse<EnrollmentResponse>> EnrolCourseAsync(Guid userId, EnrollmentRequest request);
        Task<PagedResponse<IEnumerable<EnrollmentResponse>>> GetEnrolledCoursesAsync(Guid userId, string lang, PagingRequest request);
    }
}
