using BLL.IServices.Payment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Purchases
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PurchaseService> _logger;
        public PurchaseService(IUnitOfWork unitOfWork, IPaymentService paymentService, ILogger<PurchaseService> logger)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _logger = logger;
        }

        public Task<BaseResponse<object>> CheckCourseAccessAsync(Guid userId, Guid courseId)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<PaymentCreateResponse>> CreatePaymentForPurchaseAsync(Guid userId, Guid purchaseId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return BaseResponse<PaymentCreateResponse>.Fail(new object(), "Access denied", 403);
                }

                var purchase = await _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                    .Where(p => p.PurchasesId == purchaseId && p.UserId == userId)
                    .FirstOrDefaultAsync();

                if (purchase == null)
                {
                    return BaseResponse<PaymentCreateResponse>.Fail("Purchase order not found");
                }

                if (purchase.Status != PurchaseStatus.Pending)
                {
                    return BaseResponse<PaymentCreateResponse>.Fail("The order is not in payment status");
                }

                return await _paymentService.CreatePaymentAsync(purchaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating payment: {Message}", ex.Message);
                return BaseResponse<PaymentCreateResponse>.Error("System error while creating payment");
            }
        }

        public Task<BaseResponse<object>> CreateRefundRequestAsync(Guid userId, CreateRefundRequestDto request)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<object>> ProcessPaymentFailedAsync(Guid purchaseId)
        {
            throw new NotImplementedException();
        }

        public Task<BaseResponse<object>> ProcessPaymentSuccessAsync(Guid purchaseId, string transactionReference)
        {
            throw new NotImplementedException();
        }

        public async Task<BaseResponse<PurchaseCourseResponse>> PurchaseCourseAsync(Guid userId, PurchaseCourseRequest request)
        {
            var strategy = _unitOfWork.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user == null || !user.IsEmailConfirmed || !user.Status)
                    {
                        return BaseResponse<PurchaseCourseResponse>.Fail(new object(), "Access denied", 403);
                    }

                    var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
                    if (course == null || course.Status != CourseStatus.Published)
                    {
                        return BaseResponse<PurchaseCourseResponse>.Fail("Course does not exist or is not available");
                    }

                    var existingPurchase = await _unitOfWork.Purchases.FindAllAsync(
                        p => p.UserId == userId && p.CourseId == request.CourseId && p.Status == PurchaseStatus.Completed);

                    if (existingPurchase.Any())
                    {
                        var activePurchase = existingPurchase.First();
                        var accessCheck = await CheckCourseAccessInternalAsync(userId, request.CourseId);
                        if (accessCheck.HasAccess)
                        {
                            return BaseResponse<PurchaseCourseResponse>.Fail("You already own this course");
                        }
                    }

                    var finalPrice = course.DiscountPrice ?? course.Price;
                    var now = TimeHelper.GetVietnamTime();

                    var purchase = new Purchase
                    {
                        PurchasesId = Guid.NewGuid(),
                        UserId = userId,
                        CourseId = request.CourseId,
                        TotalAmount = course.Price,
                        DiscountAmount = course.Price - finalPrice,
                        FinalAmount = finalPrice,
                        StartsAt = now,
                        ExpiresAt = now.AddDays(course.DurationDays),
                        EligibleForRefundUntil = now.AddDays(3),
                        PaymentMethod = request.PaymentMethod,
                        CreatedAt = now,
                    };

                    await _unitOfWork.Purchases.CreateAsync(purchase);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    var response = new PurchaseCourseResponse
                    {
                        PurchaseId = purchase.PurchasesId,
                        TotalAmount = purchase.TotalAmount,
                        FinalAmount = purchase.FinalAmount,
                        StartsAt = purchase.StartsAt.Value.ToString("dd-MM-yyyy"),
                        ExpiresAt = purchase.ExpiresAt.Value.ToString("dd-MM-yyyy"),
                        PurchaseStatus = purchase.Status.ToString(),
                    };

                    return BaseResponse<PurchaseCourseResponse>.Success(response);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error when purchasing course: {Message}", ex.Message);
                    return BaseResponse<PurchaseCourseResponse>.Error("System error while processing course purchase");
                }
            });
        }
        #region Private Methods
        private async Task<CourseAccessResponse> CheckCourseAccessInternalAsync(Guid userId, Guid courseId)
        {
            var purchase = await _unitOfWork.Purchases
                .FindAllAsync(p => p.UserId == userId &&
                   p.CourseId == courseId &&
                   p.Status == PurchaseStatus.Completed);

            if (!purchase.Any())
            {
                return new CourseAccessResponse { HasAccess = false, AccessStatus = "NOT_PURCHASED" };
            }

            var completedPurchase = purchase.First();
            var now = TimeHelper.GetVietnamTime();

            if (completedPurchase.ExpiresAt.HasValue && now > completedPurchase.ExpiresAt.Value)
            {
                return new CourseAccessResponse
                {
                    HasAccess = false,
                    AccessStatus = "EXPIRED",
                    ExpiresAt = completedPurchase.ExpiresAt.Value.ToString("dd-MM-yyyy"),
                    DaysRemaining = 0,
                    PurchaseId = completedPurchase.PurchasesId
                };
            }
            bool isRefundEligible = completedPurchase.EligibleForRefundUntil.HasValue &&
                                   now <= completedPurchase.EligibleForRefundUntil.Value;

            var daysRemaining = completedPurchase.ExpiresAt.HasValue ?
                (int)(completedPurchase.ExpiresAt.Value - now).TotalDays : -1;

            return new CourseAccessResponse
            {
                HasAccess = true,
                AccessStatus = isRefundEligible ? "ACTIVE_WITH_REFUND_ELIGIBLE" : "ACTIVE",
                ExpiresAt = completedPurchase.ExpiresAt?.ToString("dd-MM-yyyy"),
                DaysRemaining = daysRemaining,
                PurchaseId = completedPurchase.PurchasesId,
                RefundEligibleUntil = completedPurchase.EligibleForRefundUntil?.ToString("dd-MM-yyyy")
            };
        }
        #endregion
    }

}
