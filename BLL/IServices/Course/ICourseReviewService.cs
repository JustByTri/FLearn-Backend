using Common.DTO.ApiResponse;
using Common.DTO.CourseReview.Request;
using Common.DTO.CourseReview.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Course
{
    public interface ICourseReviewService
    {
        Task<BaseResponse<CourseReviewResponse>> CreateCourseReviewAsync(Guid userId, Guid courseId, CourseReviewRequest request);
        Task<BaseResponse<CourseReviewResponse>> UpdateCourseReviewAsync(Guid userId, Guid courseId, CourseReviewRequest request);
        Task<BaseResponse<bool>> DeleteCourseReviewAsync(Guid userId, Guid courseReviewId);
        Task<PagedResponse<List<CourseReviewResponse>>> GetCourseReviewsByCourseIdAsync(Guid courseId, PaginationParams @params);
    }
}
