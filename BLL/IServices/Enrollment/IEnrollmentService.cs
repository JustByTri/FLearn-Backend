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
        Task<BaseResponse<EnrollmentResponse>> EnrolFreeCourseAsync(Guid userId, EnrollmentRequest request);
        Task<PagedResponse<IEnumerable<EnrollmentResponse>>> GetEnrolledCoursesAsync(Guid userId, string lang, PagingRequest request);
        Task<PagedResponse<IEnumerable<EnrolledCourseOverviewResponse>>> GetEnrolledCoursesOverviewAsync(Guid userId, PagingRequest request);
        Task<BaseResponse<EnrolledCourseDetailResponse>> GetEnrolledCourseDetailAsync(Guid userId, Guid enrollmentId);
        Task<BaseResponse<EnrolledCourseCurriculumResponse>> GetEnrolledCourseCurriculumAsync(Guid userId, Guid enrollmentId);
        Task<BaseResponse<List<ContinueLearningResponse>>> GetContinueLearningAsync(Guid userId);
        Task<BaseResponse<bool>> ResumeCourseAsync(Guid userId, Guid enrollmentId, ResumeCourseRequest request);
    }
}
