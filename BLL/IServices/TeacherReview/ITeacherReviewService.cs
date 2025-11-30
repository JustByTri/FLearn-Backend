using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.TeacherReview.Request;
using Common.DTO.TeacherReview.Response;

namespace BLL.IServices.TeacherReview
{
    public interface ITeacherReviewService
    {
        Task<BaseResponse<TeacherReviewResponse>> CreateTeacherReviewAsync(Guid userId, Guid teacherId, TeacherReviewRequest request);
        Task<BaseResponse<bool>> DeleteTeacherReviewAsync(Guid userId, Guid teacherReviewId);
        Task<PagedResponse<List<TeacherReviewResponse>>> GetTeacherReviewsByTeacherIdAsync(Guid teacherId, PaginationParams @params);
        Task<BaseResponse<TeacherReviewResponse>> UpdateTeacherReviewAsync(Guid userId, Guid teacherId, TeacherReviewRequest request);
    }
}
