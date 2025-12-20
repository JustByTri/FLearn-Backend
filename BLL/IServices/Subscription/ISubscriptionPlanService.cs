using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Subscription.Request;
using Common.DTO.Subscription.Response;

namespace BLL.IServices.Subscription
{
    public interface ISubscriptionPlanService
    {
        Task<PagedResponse<IEnumerable<SubscriptionPlanResponse>>> GetAllPlansAsync(PagingRequest request);
        Task<BaseResponse<SubscriptionPlanResponse>> GetPlanByIdAsync(int id);
        Task<BaseResponse<SubscriptionPlanResponse>> CreatePlanAsync(SubscriptionPlanRequest request);
        Task<BaseResponse<SubscriptionPlanResponse>> UpdatePlanAsync(int id, SubscriptionPlanRequest request);
        Task<BaseResponse<bool>> DeletePlanAsync(int id);
    }
}
