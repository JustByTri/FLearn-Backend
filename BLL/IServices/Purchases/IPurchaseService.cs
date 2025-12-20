using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund.Request;
using Common.DTO.Refund.Response;
using System.Collections.Generic;
using System.Threading.Tasks;

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

        // NEW: Class purchase APIs
        Task<PagedResponse<List<ClassPurchaseResponse>>> GetClassPurchasesAsync(Guid userId, PurchasePagingRequest request);
        Task<BaseResponse<ClassPurchaseResponse>> GetClassPurchaseDetailAsync(Guid userId, Guid purchaseId);
    }

    // DTO trả về cho purchase class
    public class ClassPurchaseResponse
    {
        public Guid PurchaseId { get; set; }
        public Guid ClassId { get; set; }
        public string ClassTitle { get; set; } = string.Empty;
        public string ClassDescription { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string LevelName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public decimal FinalAmount { get; set; }
        public decimal? DiscountAmount { get; set; }
        public string? Status { get; set; }
        public string? PaymentMethod { get; set; }
        public string? CreatedAt { get; set; }
        public string? PaidAt { get; set; }
        public string? StartsAt { get; set; }
        public string? EndsAt { get; set; }
        public string? EligibleForRefundUntil { get; set; }
        public int DaysRemaining { get; set; }
        public bool IsRefundEligible { get; set; }
        public bool IsActive { get; set; }
        public Guid? ClassEnrollmentId { get; set; }
        public string EnrollmentStatus { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string GoogleMeetLink { get; set; } = string.Empty;
    }
}
