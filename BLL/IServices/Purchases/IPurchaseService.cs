using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund.Request;

namespace BLL.IServices.Purchases
{
    public interface IPurchaseService
    {
        Task<BaseResponse<PurchaseCourseResponse>> PurchaseCourseAsync(Guid userId, PurchaseCourseRequest request);
        Task<BaseResponse<PaymentCreateResponse>> CreatePaymentForPurchaseAsync(Guid userId, Guid purchaseId);
        Task<BaseResponse<CourseAccessResponse>> CheckCourseAccessAsync(Guid userId, Guid courseId);
        Task<BaseResponse<object>> CreateRefundRequestAsync(Guid userId, CreateRefundRequest request);
    }
}
