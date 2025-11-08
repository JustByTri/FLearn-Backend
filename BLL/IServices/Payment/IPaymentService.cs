using Common.DTO.ApiResponse;
using Common.DTO.Payment.Response;

namespace BLL.IServices.Payment
{
    public interface IPaymentService
    {
        Task<BaseResponse<PaymentCreateResponse>> CreatePaymentAsync(Guid purchaseId);
        Task<BaseResponse<object>> HandleCallbackAsync(PayOSWebhookBody payload);
        Task<BaseResponse<object>> VerifyPaymentAsync(string transactionReference);
        Task<BaseResponse<object>> ProcessRefundAsync(Guid paymentTransactionId, decimal amount);
        Task ProcessPaymentFailedAsync(string transactionRef);
    }
}
