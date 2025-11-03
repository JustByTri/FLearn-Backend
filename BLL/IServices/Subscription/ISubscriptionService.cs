using Common.DTO.Subscription;

namespace BLL.IServices.Subscription
{
 public interface ISubscriptionService
 {
 Task<SubscriptionPurchaseResponseDto> CreateSubscriptionPurchaseAsync(Guid userId, string plan);
 Task<bool> HandleCallbackAsync(SubscriptionCallbackDto callback);
 }
}
