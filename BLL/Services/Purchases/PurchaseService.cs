using BLL.IServices.Payment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund.Request;
using DAL.Helpers;
using DAL.Migrations;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

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
            return await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user == null || !user.IsEmailConfirmed || !user.Status)
                    {
                        return BaseResponse<object>.Fail(new object(), "Access denied", 403);
                    }

                    var purchase = await _unitOfWork.Purchases.Query()
                        .FirstOrDefaultAsync(p => p.PurchasesId == request.PurchaseId && p.UserId == userId);

                    if (purchase == null)
                    {
                        return BaseResponse<object>.Fail("Purchase order not found or access denied");
                    }

                    if (purchase.Status != PurchaseStatus.Completed)
                    {
                        return BaseResponse<object>.Fail("Refunds can only be requested for paid orders");
                    }

                    var now = TimeHelper.GetVietnamTime();
                    if (!purchase.EligibleForRefundUntil.HasValue || now > purchase.EligibleForRefundUntil.Value)
                    {
                        return BaseResponse<object>.Fail("The refund request deadline has passed");
                    }

                    var existingRefundRequest = await _unitOfWork.RefundRequests.Query()
                        .FirstOrDefaultAsync(r => r.PurchaseId == request.PurchaseId &&
                                                 r.Status == RefundRequestStatus.Pending);

                    if (existingRefundRequest != null)
                    {
                        return BaseResponse<object>.Fail("There is already a pending refund request for this purchase");
                    }

                    var approvedRefundRequest = await _unitOfWork.RefundRequests.Query()
                        .FirstOrDefaultAsync(r => r.PurchaseId == request.PurchaseId &&
                                                 r.Status == RefundRequestStatus.Approved);

                    if (approvedRefundRequest != null)
                    {
                        return BaseResponse<object>.Fail("This purchase has already been refunded");
                    }

                    var refundAmount = purchase.FinalAmount;

                    var refundRequest = new RefundRequest
                    {
                        RefundRequestID = Guid.NewGuid(),
                        PurchaseId = request.PurchaseId,
                        StudentID = userId,
                        Reason = request.Reason,
                        BankAccountNumber = request.BankAccountNumber,
                        BankName = request.BankName,
                        BankAccountHolderName = request.BankAccountHolderName,
                        RequestedAt = now,
                        RefundAmount = refundAmount,
                        Status = RefundRequestStatus.Pending,
                        CreatedAt = now
                    };

                    await _unitOfWork.RefundRequests.CreateAsync(refundRequest);
                    await _unitOfWork.SaveChangesAsync();

                    _logger.LogInformation("Refund request created for purchase {PurchaseId} by user {UserId}",
                        request.PurchaseId, userId);

                    return BaseResponse<object>.Success("Refund request submitted successfully");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating refund request: {Message}", ex.Message);
                    return BaseResponse<object>.Error("System error while creating refund request");
                }
            });
        }
        public async Task<BaseResponse<PurchaseDetailResponse>> GetPurchaseByIdAsync(Guid purchaseId, Guid userId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return BaseResponse<PurchaseDetailResponse>.Fail(new object(), "Access denied", 403);
                }

                var purchaseData = await _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Language)
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Level)
                    .Include(p => p.Enrollment)
                    .Where(p => p.PurchasesId == purchaseId && p.UserId == userId)
                    .Select(p => new
                    {
                        Purchase = p,
                        Course = p.Course,
                        Language = p.Course.Language,
                        Level = p.Course.Level,
                        Enrollment = p.Enrollment
                    })
                    .FirstOrDefaultAsync();

                if (purchaseData == null)
                {
                    return BaseResponse<PurchaseDetailResponse>.Fail("Purchase not found");
                }

                var now = TimeHelper.GetVietnamTime();
                var purchase = purchaseData.Purchase;
                var course = purchaseData.Course;
                var language = purchaseData.Language;
                var level = purchaseData.Level;
                var enrollment = purchaseData.Enrollment;

                var response = new PurchaseDetailResponse
                {
                    PurchaseId = purchase.PurchasesId,
                    CourseId = purchase.CourseId ?? Guid.Empty,
                    CourseName = course?.Title ?? "Unknown Course",
                    CourseDescription = course?.Description ?? string.Empty,
                    CourseThumbnail = course?.ImageUrl ?? string.Empty,
                    CoursePrice = course?.Price ?? 0,
                    CourseDiscountPrice = course?.DiscountPrice,
                    CourseDurationDays = course?.DurationDays ?? 0,
                    CourseLevel = level?.Name ?? "Unknown Level",
                    CourseLanguage = language?.LanguageName ?? "Unknown Language",
                    TotalAmount = purchase.TotalAmount,
                    DiscountAmount = purchase.DiscountAmount ?? 0,
                    FinalAmount = purchase.FinalAmount,
                    PurchaseStatus = purchase.Status.ToString(),
                    PaymentMethod = purchase.PaymentMethod.ToString() ?? "Unknown",
                    CreatedAt = purchase.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    StartsAt = purchase.StartsAt?.ToString("dd-MM-yyyy"),
                    ExpiresAt = purchase.ExpiresAt?.ToString("dd-MM-yyyy"),
                    EligibleForRefundUntil = purchase.EligibleForRefundUntil?.ToString("dd-MM-yyyy"),
                    IsRefundEligible = purchase.EligibleForRefundUntil.HasValue &&
                                      now <= purchase.EligibleForRefundUntil.Value,
                    DaysRemaining = purchase.ExpiresAt.HasValue ?
                        (int)(purchase.ExpiresAt.Value - now).TotalDays : 0,
                    EnrollmentId = purchase.EnrollmentId,
                    EnrollmentStatus = enrollment?.Status.ToString() ?? "No Enrollment"
                };

                return BaseResponse<PurchaseDetailResponse>.Success(response, "Purchase details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase by id {PurchaseId} for user {UserId}", purchaseId, userId);
                return BaseResponse<PurchaseDetailResponse>.Error("System error while retrieving purchase details");
            }
        }
        public async Task<PagedResponse<List<PurchaseDetailResponse>>> GetPurchaseDetailsByUserIdAsync(Guid userId, PagingRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return PagedResponse<List<PurchaseDetailResponse>>.Fail(new object(), "Access denied", 403);
                }

                var query = _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Language)
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Level)
                    .Include(p => p.Enrollment)
                    .Where(p => p.UserId == userId)
                    .OrderByDescending(p => p.CreatedAt)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(p => p.Course.Title.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<PurchaseStatus>(request.Status, out var status))
                {
                    query = query.Where(p => p.Status == status);
                }

                var totalItems = await query.CountAsync();

                var purchaseData = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(p => new
                    {
                        Purchase = p,
                        Course = p.Course,
                        Language = p.Course.Language,
                        Level = p.Course.Level,
                        Enrollment = p.Enrollment
                    })
                    .ToListAsync();

                var now = TimeHelper.GetVietnamTime();
                var purchases = purchaseData.Select(x =>
                {
                    var course = x.Course;
                    var language = x.Language;
                    var level = x.Level;
                    var enrollment = x.Enrollment;
                    var purchase = x.Purchase;

                    return new PurchaseDetailResponse
                    {
                        PurchaseId = purchase.PurchasesId,
                        CourseId = purchase.CourseId ?? Guid.Empty,
                        CourseName = course?.Title ?? "Unknown Course",
                        CourseDescription = course?.Description ?? string.Empty,
                        CourseThumbnail = course?.ImageUrl ?? string.Empty,
                        CoursePrice = course?.Price ?? 0,
                        CourseDiscountPrice = course?.DiscountPrice,
                        CourseDurationDays = course?.DurationDays ?? 0,
                        CourseLevel = level?.Name ?? "Unknown Level",
                        CourseLanguage = language?.LanguageName ?? "Unknown Language",
                        TotalAmount = purchase.TotalAmount,
                        DiscountAmount = purchase.DiscountAmount ?? 0,
                        FinalAmount = purchase.FinalAmount,
                        PurchaseStatus = purchase.Status.ToString(),
                        PaymentMethod = purchase.PaymentMethod.ToString() ?? "Unknown",
                        CreatedAt = purchase.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                        StartsAt = purchase.StartsAt?.ToString("dd-MM-yyyy"),
                        ExpiresAt = purchase.ExpiresAt?.ToString("dd-MM-yyyy"),
                        EligibleForRefundUntil = purchase.EligibleForRefundUntil?.ToString("dd-MM-yyyy"),
                        IsRefundEligible = purchase.EligibleForRefundUntil.HasValue &&
                                          now <= purchase.EligibleForRefundUntil.Value,
                        DaysRemaining = purchase.ExpiresAt.HasValue ?
                            (int)(purchase.ExpiresAt.Value - now).TotalDays : 0,
                        EnrollmentId = purchase.EnrollmentId,
                        EnrollmentStatus = enrollment?.Status.ToString() ?? "No Enrollment"
                    };
                }).ToList();

                return PagedResponse<List<PurchaseDetailResponse>>.Success(
                    purchases,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    $"Found {purchases.Count} purchase details"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting purchase details for user {UserId}", userId);
                return PagedResponse<List<PurchaseDetailResponse>>.Error("System error while retrieving purchase details");
            }
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

                    if (course.CourseType != CourseType.Paid)
                        return BaseResponse<PurchaseCourseResponse>.Fail(new object(), "This course is free and cannot be purchased", 400);

                    var allPurchases = await _unitOfWork.Purchases.FindAllAsync(p => p.UserId == userId && p.CourseId == request.CourseId);

                    if (allPurchases.Any())
                    {
                        var latestPurchase = allPurchases.OrderByDescending(p => p.CreatedAt).First();

                        var now = TimeHelper.GetVietnamTime();

                        var isExpired = latestPurchase.ExpiresAt.HasValue &&
                                       now > latestPurchase.ExpiresAt.Value;

                        var allowNewPurchase = latestPurchase.Status == PurchaseStatus.Failed ||
                                              latestPurchase.Status == PurchaseStatus.Cancelled ||
                                              latestPurchase.Status == PurchaseStatus.Refunded ||
                                              (latestPurchase.Status == PurchaseStatus.Completed && isExpired);

                        if (!allowNewPurchase)
                        {
                            var accessCheck = await CheckCourseAccessInternalAsync(userId, request.CourseId);

                            var purchaseResponse = new PurchaseCourseResponse
                            {
                                PurchaseId = accessCheck.PurchaseId ?? Guid.Empty,
                                ExpiresAt = accessCheck.ExpiresAt,
                                PurchaseStatus = accessCheck.AccessStatus
                            };

                            if (accessCheck.HasAccess)
                            {
                                return BaseResponse<PurchaseCourseResponse>.Success(purchaseResponse, "You already own this course", 200);
                            }
                            else
                            {
                                return BaseResponse<PurchaseCourseResponse>.Success(purchaseResponse, "Order has been created", 200);
                            }
                        }
                        else
                        {
                            _logger.LogInformation("Allowing new purchase for user {UserId}, course {CourseId} because latest purchase status is {Status} and expired: {IsExpired}",
                                userId, request.CourseId, latestPurchase.Status, isExpired);
                        }
                    }

                    var finalPrice = course.DiscountPrice ?? course.Price;

                    var purchase = new Purchase
                    {
                        PurchasesId = Guid.NewGuid(),
                        UserId = userId,
                        CourseId = request.CourseId,
                        TotalAmount = course.Price,
                        DiscountAmount = course.Price - finalPrice,
                        FinalAmount = finalPrice,
                        StartsAt = TimeHelper.GetVietnamTime(),
                        ExpiresAt = TimeHelper.GetVietnamTime().AddDays(course.DurationDays),
                        EligibleForRefundUntil = TimeHelper.GetVietnamTime().AddDays(3),
                        PaymentMethod = request.PaymentMethod,
                        CreatedAt = TimeHelper.GetVietnamTime(),
                        Status = PurchaseStatus.Pending
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

                    return BaseResponse<PurchaseCourseResponse>.Success(response, "Purchase order created successfully");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    _logger.LogError(ex, "Error when purchasing course: {Message}", ex.Message);
                    return BaseResponse<PurchaseCourseResponse>.Error("System error while processing course purchase");
                }
            });
        }
        public async Task<PagedResponse<List<CoursePurchaseResponse>>> GetCoursePurchasesByLanguageAsync(Guid userId, PurchasePagingRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return PagedResponse<List<CoursePurchaseResponse>>.Fail(new object(), "Access denied", 403);
                }

                if (user.ActiveLanguageId == null)
                    return PagedResponse<List<CoursePurchaseResponse>>.Fail(new object(), "User has no active language set", 400);

                PurchaseStatus? purchaseStatus = null;

                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    if (!Enum.TryParse<PurchaseStatus>(request.Status.Trim(), true, out var status))
                    {
                        return PagedResponse<List<CoursePurchaseResponse>>
                            .Fail(new object(), "Invalid status", 400);
                    }

                    purchaseStatus = status;
                }

                return await GetCoursePurchasesByLanguageInternalAsync(userId, user.ActiveLanguageId.Value, request.PageNumber, request.PageSize, purchaseStatus, request.ActiveOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course purchases for user {UserId}", userId);
                return PagedResponse<List<CoursePurchaseResponse>>.Error("System error while retrieving course purchases");
            }
        }
        public async Task<BaseResponse<CoursePurchaseResponse>> GetCoursePurchaseDetailAsync(Guid userId, Guid purchaseId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return BaseResponse<CoursePurchaseResponse>.Fail(new object(), "Access denied", 403);
                }

                var purchaseEntity = await _unitOfWork.Purchases.Query()
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Language)
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Level)
                    .Include(p => p.Course)
                        .ThenInclude(c => c.Teacher)
                        .ThenInclude(t => t.User)
                    .Include(p => p.Enrollment)
                    .Where(p => p.PurchasesId == purchaseId &&
                                p.UserId == userId &&
                                p.CourseId != null)
                    .FirstOrDefaultAsync();

                if (purchaseEntity == null)
                    return BaseResponse<CoursePurchaseResponse>.Fail(new object(), "Purchase not found", 404);

                var now = TimeHelper.GetVietnamTime();

                var purchase = new CoursePurchaseResponse
                {
                    PurchaseId = purchaseEntity.PurchasesId,
                    CourseId = purchaseEntity.CourseId.Value,
                    CourseTitle = purchaseEntity.Course.Title,
                    CourseDescription = purchaseEntity.Course.Description,
                    CourseThumbnail = purchaseEntity.Course.ImageUrl,
                    LanguageName = purchaseEntity.Course.Language.LanguageName,
                    LevelName = purchaseEntity.Course.Level.Name,
                    Price = purchaseEntity.TotalAmount,
                    DiscountPrice = purchaseEntity.Course.DiscountPrice,
                    FinalAmount = purchaseEntity.FinalAmount,
                    DiscountAmount = purchaseEntity.DiscountAmount,
                    Status = purchaseEntity.Status.ToString(),
                    PaymentMethod = purchaseEntity.PaymentMethod.ToString(),
                    CreatedAt = purchaseEntity.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    PaidAt = purchaseEntity.PaidAt?.ToString("dd-MM-yyyy HH:mm"),
                    StartsAt = purchaseEntity.StartsAt?.ToString("dd-MM-yyyy HH:mm"),
                    ExpiresAt = purchaseEntity.ExpiresAt?.ToString("dd-MM-yyyy HH:mm"),
                    EligibleForRefundUntil = purchaseEntity.EligibleForRefundUntil?.ToString("dd-MM-yyyy"),
                    DaysRemaining = purchaseEntity.ExpiresAt.HasValue ? (int)(purchaseEntity.ExpiresAt.Value - now).TotalDays : -1,
                    IsRefundEligible = purchaseEntity.EligibleForRefundUntil.HasValue && now <= purchaseEntity.EligibleForRefundUntil.Value,
                    IsActive = purchaseEntity.Status == PurchaseStatus.Completed &&
                               (!purchaseEntity.ExpiresAt.HasValue || purchaseEntity.ExpiresAt.Value > now),
                    EnrollmentId = purchaseEntity.EnrollmentId,
                    EnrollmentStatus = purchaseEntity.Enrollment != null ? purchaseEntity.Enrollment.Status.ToString() : "No Enrollment",
                    CourseDetails = new CourseDetailResponse
                    {
                        CourseId = purchaseEntity.Course.CourseID,
                        Title = purchaseEntity.Course.Title,
                        Description = purchaseEntity.Course.Description,
                        ImageUrl = purchaseEntity.Course.ImageUrl,
                        LanguageName = purchaseEntity.Course.Language.LanguageName,
                        LevelName = purchaseEntity.Course.Level.Name,
                        CourseType = purchaseEntity.Course.CourseType.ToString(),
                        GradingType = purchaseEntity.Course.GradingType.ToString(),
                        NumLessons = purchaseEntity.Course.NumLessons,
                        NumUnits = purchaseEntity.Course.NumUnits,
                        DurationDays = purchaseEntity.Course.DurationDays,
                        EstimatedHours = purchaseEntity.Course.EstimatedHours,
                        AverageRating = purchaseEntity.Course.AverageRating,
                        ReviewCount = purchaseEntity.Course.ReviewCount,
                        LearnerCount = purchaseEntity.Course.LearnerCount,
                        TeacherName = purchaseEntity.Course.Teacher.User.FullName ?? purchaseEntity.Course.Teacher.User.UserName,
                        TeacherAvatar = purchaseEntity.Course.Teacher.Avatar ?? string.Empty
                    }
                };

                return BaseResponse<CoursePurchaseResponse>.Success(purchase, "Purchase detail retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting course purchase detail for purchase {PurchaseId}", purchaseId);
                return BaseResponse<CoursePurchaseResponse>.Error("System error while retrieving purchase detail");
            }
        }
        public async Task<PagedResponse<List<SubscriptionPurchaseResponse>>> GetSubscriptionPurchasesAsync(Guid userId, PurchasePagingRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);

                if (user == null || !user.IsEmailConfirmed || !user.Status)
                    return PagedResponse<List<SubscriptionPurchaseResponse>>.Fail(new object(), "Access denied", 403);

                if (user.ActiveLanguageId == null)
                    return PagedResponse<List<SubscriptionPurchaseResponse>>.Fail(new object(), "User has no active language set", 400);

                PurchaseStatus? status = null;
                if (!string.IsNullOrWhiteSpace(request.Status))
                {
                    if (!Enum.TryParse<PurchaseStatus>(request.Status.Trim(), true, out var parsedStatus))
                    {
                        return PagedResponse<List<SubscriptionPurchaseResponse>>.Fail(new object(), "Invalid status", 400);
                    }
                    status = parsedStatus;
                }

                return await GetSubscriptionPurchasesInternalAsync(userId, request.PageNumber, request.PageSize, status, request.ActiveOnly);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription purchases for user {UserId}", userId);
                return PagedResponse<List<SubscriptionPurchaseResponse>>.Error("System error while retrieving subscription purchases");
            }
        }
        public async Task<BaseResponse<SubscriptionPurchaseResponse>> GetSubscriptionPurchaseDetailAsync(Guid userId, Guid purchaseId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                    return BaseResponse<SubscriptionPurchaseResponse>.Fail(new object(), "Access denied", 403);

                var now = TimeHelper.GetVietnamTime();

                var purchaseEntity = await _unitOfWork.Purchases.Query()
                    .Include(p => p.Subscription)
                    .Where(p => p.PurchasesId == purchaseId &&
                                p.UserId == userId &&
                                p.SubscriptionId != null)
                    .FirstOrDefaultAsync();

                if (purchaseEntity == null)
                    return BaseResponse<SubscriptionPurchaseResponse>.Fail(new object(), "Purchase not found", 404);

                var purchase = new SubscriptionPurchaseResponse
                {
                    PurchaseId = purchaseEntity.PurchasesId,
                    SubscriptionId = purchaseEntity.SubscriptionId.Value,
                    SubscriptionType = purchaseEntity.Subscription.SubscriptionType,
                    ConversationQuota = purchaseEntity.Subscription.ConversationQuota,
                    Price = purchaseEntity.TotalAmount,
                    FinalAmount = purchaseEntity.FinalAmount,
                    DiscountAmount = purchaseEntity.DiscountAmount,
                    Status = purchaseEntity.Status.ToString(),
                    PaymentMethod = purchaseEntity.PaymentMethod.ToString(),
                    CreatedAt = purchaseEntity.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                    PaidAt = purchaseEntity.PaidAt?.ToString("dd-MM-yyyy HH:mm"),
                    StartsAt = purchaseEntity.StartsAt?.ToString("dd-MM-yyyy HH:mm"),
                    ExpiresAt = purchaseEntity.ExpiresAt?.ToString("dd-MM-yyyy HH:mm"),
                    EligibleForRefundUntil = purchaseEntity.EligibleForRefundUntil?.ToString("dd-MM-yyyy"),
                    DaysRemaining = purchaseEntity.ExpiresAt.HasValue ? (int)(purchaseEntity.ExpiresAt.Value - now).TotalDays : -1,
                    IsRefundEligible = purchaseEntity.EligibleForRefundUntil.HasValue && now <= purchaseEntity.EligibleForRefundUntil.Value,
                    IsActive = purchaseEntity.Status == PurchaseStatus.Completed && (!purchaseEntity.ExpiresAt.HasValue || purchaseEntity.ExpiresAt.Value > now),
                    SubscriptionDetails = new SubscriptionDetailResponse
                    {
                        SubscriptionId = purchaseEntity.Subscription.SubscriptionID,
                        SubscriptionType = purchaseEntity.Subscription.SubscriptionType,
                        ConversationQuota = purchaseEntity.Subscription.ConversationQuota,
                        StartDate = purchaseEntity.Subscription.StartDate.ToString("dd-MM-yyyy HH:mm"),
                        EndDate = purchaseEntity.Subscription.EndDate?.ToString("dd-MM-yyyy HH:mm"),
                        IsActive = purchaseEntity.Subscription.IsActive,
                        ConversationsUsed = 0,
                        ConversationsRemaining = purchaseEntity.Subscription.ConversationQuota
                    }
                };

                return BaseResponse<SubscriptionPurchaseResponse>.Success(purchase, "Purchase detail retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subscription purchase detail for purchase {PurchaseId}", purchaseId);
                return BaseResponse<SubscriptionPurchaseResponse>.Error("System error while retrieving purchase detail");
            }
        }
        #region Private Methods
        private async Task<PagedResponse<List<SubscriptionPurchaseResponse>>> GetSubscriptionPurchasesInternalAsync(Guid userId, int page, int pageSize, PurchaseStatus? status = null, bool? activeOnly = null)
        {
            var now = TimeHelper.GetVietnamTime();

            var query = _unitOfWork.Purchases.Query()
                .Include(p => p.Subscription)
                .Where(p => p.UserId == userId && p.SubscriptionId != null);

            if (status.HasValue)
                query = query.Where(p => p.Status == status.Value);

            if (activeOnly.HasValue && activeOnly.Value)
                query = query.Where(p => p.Status == PurchaseStatus.Completed && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > now));

            var totalCount = await query.CountAsync();

            var purchasesList = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var purchases = purchasesList.Select(p => new SubscriptionPurchaseResponse
            {
                PurchaseId = p.PurchasesId,
                SubscriptionId = p.SubscriptionId.Value,
                SubscriptionType = p.Subscription.SubscriptionType,
                ConversationQuota = p.Subscription.ConversationQuota,
                Price = p.TotalAmount,
                FinalAmount = p.FinalAmount,
                DiscountAmount = p.DiscountAmount,
                Status = p.Status.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                CreatedAt = p.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                PaidAt = p.PaidAt?.ToString("dd-MM-yyyy HH:mm"),
                StartsAt = p.StartsAt?.ToString("dd-MM-yyyy HH:mm"),
                ExpiresAt = p.ExpiresAt?.ToString("dd-MM-yyyy HH:mm"),
                EligibleForRefundUntil = p.EligibleForRefundUntil?.ToString("dd-MM-yyyy"),
                DaysRemaining = p.ExpiresAt.HasValue ? (int)(p.ExpiresAt.Value - now).TotalDays : -1,
                IsRefundEligible = p.EligibleForRefundUntil.HasValue && now <= p.EligibleForRefundUntil.Value,
                IsActive = p.Status == PurchaseStatus.Completed && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > now)
            }).ToList();

            return PagedResponse<List<SubscriptionPurchaseResponse>>.Success(purchases, page, pageSize, totalCount, "Subscription purchases retrieved successfully");
        }
        private async Task<PagedResponse<List<CoursePurchaseResponse>>> GetCoursePurchasesByLanguageInternalAsync(
           Guid userId,
           Guid languageId,
           int page,
           int pageSize,
           PurchaseStatus? status = null,
           bool? activeOnly = null)
        {
            var now = TimeHelper.GetVietnamTime();

            // Base query
            var query = _unitOfWork.Purchases.Query()
                .Include(p => p.Course)
                    .ThenInclude(c => c.Language)
                .Include(p => p.Course)
                    .ThenInclude(c => c.Level)
                .Include(p => p.Enrollment)
                .Where(p => p.UserId == userId &&
                           p.CourseId != null &&
                           p.Course.LanguageId == languageId);

            if (status.HasValue)
            {
                query = query.Where(p => p.Status == status.Value);
            }

            if (activeOnly.HasValue && activeOnly.Value)
            {
                query = query.Where(p => p.Status == PurchaseStatus.Completed &&
                                         (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > now));
            }

            var totalCount = await query.CountAsync();

            // Materialize first to avoid EF Core translation issues
            var purchasesList = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Project in memory
            var purchases = purchasesList.Select(p => new CoursePurchaseResponse
            {
                PurchaseId = p.PurchasesId,
                CourseId = p.CourseId.Value,
                CourseTitle = p.Course.Title,
                CourseDescription = p.Course.Description,
                CourseThumbnail = p.Course.ImageUrl,
                LanguageName = p.Course.Language.LanguageName,
                LevelName = p.Course.Level.Name,
                Price = p.TotalAmount,
                DiscountPrice = p.Course.DiscountPrice,
                FinalAmount = p.FinalAmount,
                DiscountAmount = p.DiscountAmount,
                Status = p.Status.ToString(),
                PaymentMethod = p.PaymentMethod.ToString(),
                CreatedAt = p.CreatedAt.ToString("dd-MM-yyyy HH:mm"),
                PaidAt = p.PaidAt?.ToString("dd-MM-yyyy HH:mm"),
                StartsAt = p.StartsAt?.ToString("dd-MM-yyyy HH:mm"),
                ExpiresAt = p.ExpiresAt?.ToString("dd-MM-yyyy HH:mm"),
                EligibleForRefundUntil = p.EligibleForRefundUntil?.ToString("dd-MM-yyyy"),
                DaysRemaining = p.ExpiresAt.HasValue ? (int)(p.ExpiresAt.Value - now).TotalDays : -1,
                IsRefundEligible = p.EligibleForRefundUntil.HasValue && now <= p.EligibleForRefundUntil.Value,
                IsActive = p.Status == PurchaseStatus.Completed && (!p.ExpiresAt.HasValue || p.ExpiresAt.Value > now),
                EnrollmentId = p.EnrollmentId,
                EnrollmentStatus = p.Enrollment != null ? p.Enrollment.Status.ToString() : "No Enrollment"
            }).ToList();

            return PagedResponse<List<CoursePurchaseResponse>>.Success(
                purchases, page, pageSize, totalCount, "Course purchases retrieved successfully");
        }
        private async Task<CourseAccessResponse> CheckCourseAccessInternalAsync(Guid userId, Guid courseId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return new CourseAccessResponse { HasAccess = false, AccessStatus = "USER_NOT_FOUND" };

                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null)
                    return new CourseAccessResponse { HasAccess = false, AccessStatus = "COURSE_NOT_FOUND" };

                var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == user.UserID && l.LanguageId == course.LanguageId);
                if (learner == null)
                    return new CourseAccessResponse { HasAccess = false, AccessStatus = "LEARNER_NOT_FOUND" };

                if (course.CourseType == CourseType.Free)
                {
                    var existingEnrollment = await _unitOfWork.Enrollments.FindAsync(e => e.CourseId == course.CourseID && e.LearnerId == learner.LearnerLanguageId);
                    if (existingEnrollment == null)
                        return new CourseAccessResponse { HasAccess = false, AccessStatus = "NOT_ENROLLED" };
                    else
                        return new CourseAccessResponse { HasAccess = true, AccessStatus = "ENROLLED" };
                }

                var purchase = await _unitOfWork.Purchases
                .FindAllAsync(p => p.UserId == userId &&
                   p.CourseId == courseId);

                if (!purchase.Any())
                {
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
                _logger.LogError(ex, "Error in CheckCourseAccessInternalAsync");
                throw;
            }
        }
        #endregion
    }

}
