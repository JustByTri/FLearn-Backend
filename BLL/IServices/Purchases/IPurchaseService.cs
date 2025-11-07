using Common.DTO.ApiResponse;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund;

namespace BLL.IServices.Purchases
{
    public interface IPurchaseService
    {
        Task<BaseResponse<PurchaseCourseResponse>> PurchaseCourseAsync(Guid userId, PurchaseCourseRequest request);
        Task<BaseResponse<PaymentCreateResponse>> CreatePaymentForPurchaseAsync(Guid userId, Guid purchaseId);
        Task<BaseResponse<object>> ProcessPaymentSuccessAsync(Guid purchaseId, string transactionReference);
        Task<BaseResponse<object>> ProcessPaymentFailedAsync(Guid purchaseId);
        Task<BaseResponse<object>> CheckCourseAccessAsync(Guid userId, Guid courseId);
        Task<BaseResponse<object>> CreateRefundRequestAsync(Guid userId, CreateRefundRequestDto request);
    }
}
