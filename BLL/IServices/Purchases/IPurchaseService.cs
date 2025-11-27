using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund.Request;
using Common.DTO.Refund.Response;

namespace BLL.IServices.Purchases
{
    public interface IPurchaseService
    {
        Task<BaseResponse<PurchaseCourseResponse>> PurchaseCourseAsync(Guid userId, PurchaseCourseRequest request);
        Task<BaseResponse<PaymentCreateResponse>> CreatePaymentForPurchaseAsync(Guid userId, Guid purchaseId);
        Task<BaseResponse<CourseAccessResponse>> CheckCourseAccessAsync(Guid userId, Guid courseId);
        Task<BaseResponse<object>> CreateRefundRequestAsync(Guid userId, CreateRefundRequest request);
        Task<BaseResponse<object>> ProcessRefundDecisionAsync(Guid userId, Guid refundRequestId, bool isApproved, string note);
        Task<PagedResponse<List<RefundRequestResponse>>> GetRefundRequestsAsync(Guid userId, RefundRequestFilterRequest request);
        Task<BaseResponse<RefundRequestResponse>> GetMyRefundRequestDetailAsync(Guid userId, Guid purchaseId);
        Task<PagedResponse<List<PurchaseDetailResponse>>> GetPurchaseDetailsByUserIdAsync(Guid userId, PagingRequest request);
        Task<BaseResponse<PurchaseDetailResponse>> GetPurchaseByIdAsync(Guid purchaseId, Guid userId);
        Task<PagedResponse<List<CoursePurchaseResponse>>> GetCoursePurchasesByLanguageAsync(Guid userId, PurchasePagingRequest request);
        Task<PagedResponse<List<SubscriptionPurchaseResponse>>> GetSubscriptionPurchasesAsync(Guid userId, PurchasePagingRequest request);
        Task<BaseResponse<CoursePurchaseResponse>> GetCoursePurchaseDetailAsync(Guid userId, Guid purchaseId);
        Task<BaseResponse<SubscriptionPurchaseResponse>> GetSubscriptionPurchaseDetailAsync(Guid userId, Guid purchaseId);
        Task<PagedResponse<List<RefundRequestResponse>>> GetStudentRefundRequestsByLanguageAsync(Guid userId, RefundRequestFilterRequest request);
        Task<BaseResponse<RefundRequestResponse>> GetRefundRequestDetailByIdAsync(Guid userId, Guid refundRequestId);
    }
}
