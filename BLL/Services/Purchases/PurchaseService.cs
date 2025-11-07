using BLL.IServices.Payment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund.Request;
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
        public async Task<BaseResponse<CourseAccessResponse>> CheckCourseAccessAsync(Guid userId, Guid courseId)
        {
            try
            {
                var result = await CheckCourseAccessInternalAsync(userId, courseId);
                return BaseResponse<CourseAccessResponse>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking course access");
                return BaseResponse<CourseAccessResponse>.Fail("System error while checking access rights");
            }
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
        public async Task<BaseResponse<object>> CreateRefundRequestAsync(Guid userId, CreateRefundRequest request)
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
                        return BaseResponse<object>.Fail(new object(), "Access denied", 403);
                    }

                    var purchase = await _unitOfWork.Purchases.GetByIdAsync(request.PurchaseId);
                    if (purchase == null)
                    {
                        return BaseResponse<object>.Fail("Purchase order not found");
                    }

                    if (purchase.Status != PurchaseStatus.Completed)
                    {
                        return BaseResponse<object>.Fail("Refunds can only be requested for paid orders");
                    }

                    if (TimeHelper.GetVietnamTime() > purchase.EligibleForRefundUntil)
                    {
                        return BaseResponse<object>.Fail("The refund request deadline has passed");
                    }

                    var refundAmount = purchase.FinalAmount;

                    var refundRequest = new RefundRequest
                    {
                        RefundRequestID = Guid.NewGuid(),
                        PurchaseId = request.PurchaseId,
                        Reason = request.Reason,
                        BankAccountNumber = request.BankAccountNumber,
                        BankName = request.BankName,
                        BankAccountHolderName = request.BankAccountHolderName,
                        RequestedAt = TimeHelper.GetVietnamTime(),
                        RefundAmount = refundAmount,
                        Status = RefundRequestStatus.Pending,
                        CreatedAt = TimeHelper.GetVietnamTime()
                    };

                    await _unitOfWork.RefundRequests.CreateAsync(refundRequest);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    return BaseResponse<object>.Success("Refund request submitted successfully");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error creating refund request: {Message}", ex.Message);
                    return BaseResponse<object>.Error("System error while creating refund request");
                }
            });
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
                        return BaseResponse<PurchaseCourseResponse>.Fail(new object(), "Course does not exist or is not available", 400);
                    }

                    var existingPurchase = await _unitOfWork.Purchases.FindAllAsync(
                        p => p.UserId == userId && p.CourseId == request.CourseId && p.Status == PurchaseStatus.Completed);

                    if (existingPurchase.Any())
                    {
                        var activePurchase = existingPurchase.First();
                        var accessCheck = await CheckCourseAccessInternalAsync(userId, request.CourseId);
                        if (accessCheck.HasAccess)
                        {
                            return BaseResponse<PurchaseCourseResponse>.Fail(new object(), "You already own this course", 400);
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
            var strategy = _unitOfWork.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                try
                {
                    await _unitOfWork.BeginTransactionAsync();

                    var purchase = await _unitOfWork.Purchases
                    .FindAllAsync(p => p.UserId == userId &&
                       p.CourseId == courseId);



                    if (!purchase.Any())
                    {
                        await _unitOfWork.CommitTransactionAsync();
                        return new CourseAccessResponse { HasAccess = false, AccessStatus = "NOT_PURCHASED" };
                    }

                    var latestPurchase = purchase.OrderByDescending(p => p.CreatedAt).First();
                    var now = TimeHelper.GetVietnamTime();

                    var enrollment = await _unitOfWork.Enrollments
                        .FindAsync(e => e.EnrollmentID == latestPurchase.EnrollmentId);

                    switch (latestPurchase.Status)
                    {
                        case PurchaseStatus.Refunded:

                            if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Cancelled)
                            {
                                enrollment.Status = DAL.Type.EnrollmentStatus.Cancelled;
                                await _unitOfWork.SaveChangesAsync();
                            }
                            await _unitOfWork.CommitTransactionAsync();
                            return new CourseAccessResponse
                            {
                                HasAccess = false,
                                AccessStatus = "REFUNDED",
                                PurchaseId = latestPurchase.PurchasesId
                            };

                        case PurchaseStatus.Expired:
                            if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Expired)
                            {
                                enrollment.Status = DAL.Type.EnrollmentStatus.Expired;
                                await _unitOfWork.SaveChangesAsync();
                            }
                            await _unitOfWork.CommitTransactionAsync();
                            return new CourseAccessResponse
                            {
                                HasAccess = false,
                                AccessStatus = "EXPIRED",
                                ExpiresAt = latestPurchase.ExpiresAt?.ToString("dd-MM-yyyy"),
                                DaysRemaining = 0,
                                PurchaseId = latestPurchase.PurchasesId
                            };

                        case PurchaseStatus.Failed:
                            await _unitOfWork.CommitTransactionAsync();
                            return new CourseAccessResponse
                            {
                                HasAccess = false,
                                AccessStatus = "PAYMENT_FAILED",
                                PurchaseId = latestPurchase.PurchasesId
                            };

                        case PurchaseStatus.Pending:
                            await _unitOfWork.CommitTransactionAsync();
                            return new CourseAccessResponse
                            {
                                HasAccess = false,
                                AccessStatus = "PENDING_PAYMENT",
                                PurchaseId = latestPurchase.PurchasesId
                            };
                    }

                    if (latestPurchase.ExpiresAt.HasValue && now > latestPurchase.ExpiresAt.Value)
                    {
                        latestPurchase.Status = PurchaseStatus.Expired;

                        if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Expired)
                        {
                            enrollment.Status = DAL.Type.EnrollmentStatus.Expired;
                        }

                        await _unitOfWork.SaveChangesAsync();
                        await _unitOfWork.CommitTransactionAsync();

                        return new CourseAccessResponse
                        {
                            HasAccess = false,
                            AccessStatus = "EXPIRED",
                            ExpiresAt = latestPurchase.ExpiresAt.Value.ToString("dd-MM-yyyy"),
                            DaysRemaining = 0,
                            PurchaseId = latestPurchase.PurchasesId
                        };
                    }

                    if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Active)
                    {
                        enrollment.Status = DAL.Type.EnrollmentStatus.Active;
                        await _unitOfWork.SaveChangesAsync();
                    }

                    bool isRefundEligible = latestPurchase.EligibleForRefundUntil.HasValue &&
                           now <= latestPurchase.EligibleForRefundUntil.Value;

                    var daysRemaining = latestPurchase.ExpiresAt.HasValue ?
                        (int)(latestPurchase.ExpiresAt.Value - now).TotalDays : -1;

                    await _unitOfWork.CommitTransactionAsync();

                    return new CourseAccessResponse
                    {
                        HasAccess = true,
                        AccessStatus = isRefundEligible ? "ACTIVE_WITH_REFUND_ELIGIBLE" : "ACTIVE",
                        ExpiresAt = latestPurchase.ExpiresAt?.ToString("dd-MM-yyyy"),
                        DaysRemaining = daysRemaining,
                        PurchaseId = latestPurchase.PurchasesId,
                        RefundEligibleUntil = latestPurchase.EligibleForRefundUntil?.ToString("dd-MM-yyyy")
                    };
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error in CheckCourseAccessInternalAsync");
                    throw;
                }
            });
        }
        #endregion
    }

}
