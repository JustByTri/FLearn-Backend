using Common.DTO.ApiResponse;
using Common.DTO.AppReview.Request;
using Common.DTO.AppReview.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.AppReview
{
    public interface IAppReviewService
    {
        Task<PagedResponse<List<AppReviewResponse>>> GetAllAppReviewsAsync(PaginationParams @params);
        Task<BaseResponse<AppReviewResponse>> CreateAppReviewAsync(Guid userId, AppReviewRequest request);
        Task<BaseResponse<AppReviewResponse>> UpdateAppReviewAsync(Guid userId, Guid reviewId, AppReviewRequest request);
        Task<BaseResponse<bool>> DeleteAppReviewAsync(Guid userId, Guid reviewId);
        Task<BaseResponse<AppReviewResponse>> GetMyReviewAsync(Guid userId);
    }
}
