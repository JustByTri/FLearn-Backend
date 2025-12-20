using Common.DTO.Subscription;

namespace BLL.IServices.Subscription
{
    public interface ISubscriptionService
    {
        Task<SubscriptionPurchaseResponseDto> CreateSubscriptionPurchaseAsync(Guid userId, int subscriptionPlanId);
        Task<bool> HandleCallbackAsync(SubscriptionCallbackDto callback);
    }
}
