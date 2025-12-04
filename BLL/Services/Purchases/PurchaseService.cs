using BLL.IServices.Payment;
using BLL.IServices.Purchases;
using BLL.IServices.Upload;
using BLL.IServices.Wallets;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Payment.Response;
using Common.DTO.Purchases.Request;
using Common.DTO.Purchases.Response;
using Common.DTO.Refund.Request;
using Common.DTO.Refund.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Purchases
{
    public class PurchaseService : IPurchaseService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPaymentService _paymentService;
        private readonly ILogger<PurchaseService> _logger;
        private readonly IBackgroundJobClient _backgroundJobClient;
        private readonly ICloudinaryService _cloudinaryService;
        private const decimal SYSTEM_FEE_PERCENTAGE = 0.10m;
        private const decimal TEACHER_FEE_PERCENTAGE = 0.90m;
        public PurchaseService(IUnitOfWork unitOfWork, IPaymentService paymentService, ILogger<PurchaseService> logger, IBackgroundJobClient backgroundJobClient, ICloudinaryService cloudinaryService)
        {
            _unitOfWork = unitOfWork;
            _paymentService = paymentService;
            _logger = logger;
            _backgroundJobClient = backgroundJobClient;
            _cloudinaryService = cloudinaryService;
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

                    var purchase = await _unitOfWork.Purchases.FindAsync(p => p.PurchasesId == request.PurchaseId && p.UserId == userId);

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

                    string? proofImageUrl = null;
                    if (request.ProofImage != null)
                    {
                        var uploadResult = await _cloudinaryService.UploadImageAsync(request.ProofImage);

                        if (uploadResult != null)
                        {
                            proofImageUrl = uploadResult.Url;
                        }
                        else
                        {
                            _logger.LogWarning("Failed to upload proof image for user {UserId}", userId);
                        }
                    }

                    var refundAmount = purchase.FinalAmount;

                    var refundRequest = new RefundRequest
                    {
                        RefundRequestID = Guid.NewGuid(),
                        PurchaseId = request.PurchaseId,
                        StudentID = userId,
                        RequestType = RefundRequestType.Other,
                        Reason = request.Reason,
                        BankAccountNumber = request.BankAccountNumber,
                        BankName = request.BankName,
                        BankAccountHolderName = request.BankAccountHolderName,
                        ProofImageUrl = proofImageUrl,
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
        public async Task<BaseResponse<object>> ProcessRefundDecisionAsync(Guid userId, Guid refundRequestId, bool isApproved, string note)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return BaseResponse<object>.Fail(new object(), "Access denied.", 401);

                var userRole = await _unitOfWork.UserRoles.FindAsync(x => x.UserID == userId);
                if (userRole == null)
                {
                    return BaseResponse<object>.Fail(new object(), "User role not found.", 404);
                }

                var role = await _unitOfWork.Roles.GetByIdAsync(userRole.RoleID);
                if (role == null)
                {
                    return BaseResponse<object>.Fail(new object(), "Role not found.", 404);
                }

                if (role.Name != "Admin")
                {
                    return BaseResponse<object>.Fail(new object(), "Permission denied for this role.", 403);
                }

                var refundRequest = await _unitOfWork.RefundRequests.Query()
                    .Include(r => r.Purchase)
                    .FirstOrDefaultAsync(r => r.RefundRequestID == refundRequestId);

                if (refundRequest == null)
                    return BaseResponse<object>.Fail("Refund request not found");

                if (refundRequest.Status != RefundRequestStatus.Pending)
                    return BaseResponse<object>.Fail("Request is not in pending status");

                var purchase = refundRequest.Purchase;
                if (purchase == null) return BaseResponse<object>.Fail("Purchase info missing");

                refundRequest.ProcessedByAdminID = userId;
                refundRequest.ProcessedAt = TimeHelper.GetVietnamTime();
                refundRequest.AdminNote = note;

                // =========================================================
                // TRƯỜNG HỢP 1: TỪ CHỐI HOÀN TIỀN (REJECT)
                // =========================================================
                if (!isApproved)
                {
                    refundRequest.Status = RefundRequestStatus.Rejected;
                    await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);
                    await _unitOfWork.SaveChangesAsync();

                    // Kiểm tra xem thời hạn 3 ngày đã qua chưa?
                    // Dựa vào PaidAt hoặc CreatedAt (theo logic PayOSPaymentService của bạn)
                    var paymentTime = purchase.PaidAt ?? purchase.CreatedAt;
                    var refundDeadline = paymentTime.AddDays(3);
                    var now = TimeHelper.GetVietnamTime();

                    if (now > refundDeadline)
                    {
                        // Nếu đã quá hạn 3 ngày, nghĩa là Job tự động cũ đã chạy và bị SKIP.
                        // Ta cần chạy lại nó ngay bây giờ.
                        _logger.LogInformation($"Refund rejected after 3 days. Retriggering transfer for Purchase {purchase.PurchasesId}");

                        // Gọi Job chuyển tiền (hàm này đã được bạn sửa để check trạng thái Pending/Approved ở bước trước)
                        _backgroundJobClient.Enqueue<IWalletService>(ws => ws.ProcessCourseCreationFeeTransferAsync(purchase.PurchasesId));
                    }
                    else
                    {
                        // Nếu chưa quá 3 ngày, không cần làm gì cả. 
                        // Job tự động gốc vẫn đang nằm trong hàng đợi (Scheduled) và sẽ chạy trong tương lai.
                        _logger.LogInformation($"Refund rejected within 3 days. Original scheduled job will handle transfer for Purchase {purchase.PurchasesId}");
                    }

                    return BaseResponse<object>.Success("Refund request rejected. Wallet transfer logic handled.");
                }

                // =========================================================
                // TRƯỜNG HỢP 2: CHẤP NHẬN HOÀN TIỀN (APPROVED)
                // =========================================================
                refundRequest.Status = RefundRequestStatus.Approved;
                purchase.Status = PurchaseStatus.Refunded;

                await _unitOfWork.Purchases.UpdateAsync(purchase);
                await _unitOfWork.RefundRequests.UpdateAsync(refundRequest);

                await _unitOfWork.SaveChangesAsync();
                // Tìm enrollment để hủy
                var enrollment = await _unitOfWork.Enrollments.FindAsync(e => e.EnrollmentID == purchase.EnrollmentId);

                if (enrollment != null)
                {
                    enrollment.Status = DAL.Type.EnrollmentStatus.Cancelled;
                    await _unitOfWork.Enrollments.UpdateAsync(enrollment);
                    await _unitOfWork.SaveChangesAsync();
                }

                // Logic trừ tiền Admin Wallet (Revert tiền)
                var adminWallet = await _unitOfWork.Wallets.FindAsync(w => w.OwnerType == OwnerType.Admin && w.Currency == CurrencyType.VND);

                if (adminWallet != null)
                {
                    decimal systemShare = purchase.FinalAmount * SYSTEM_FEE_PERCENTAGE;
                    decimal teacherShare = purchase.FinalAmount * TEACHER_FEE_PERCENTAGE;

                    // Kiểm tra số dư trước khi trừ (Optional nhưng recommend)
                    if (adminWallet.AvailableBalance < systemShare || adminWallet.HoldBalance < teacherShare)
                    {
                        throw new Exception("System wallet does not have enough balance to refund (Critical Error)");
                    }

                    adminWallet.AvailableBalance -= systemShare; // Trả lại phí hệ thống
                    adminWallet.HoldBalance -= teacherShare;     // Trả lại phần giữ của GV
                    adminWallet.TotalBalance -= purchase.FinalAmount;
                    adminWallet.UpdatedAt = TimeHelper.GetVietnamTime();

                    // Tạo Transaction log cho Admin Wallet
                    var walletTrans = new WalletTransaction
                    {
                        WalletTransactionId = Guid.NewGuid(),
                        WalletId = adminWallet.WalletId,
                        TransactionType = TransactionType.Payout,
                        Amount = -purchase.FinalAmount,
                        ReferenceId = refundRequest.RefundRequestID,
                        ReferenceType = ReferenceType.Refund,
                        Description = $"Refund for purchase {purchase.PurchasesId}",
                        Status = TransactionStatus.Succeeded,
                        CreatedAt = TimeHelper.GetVietnamTime()
                    };
                    await _unitOfWork.WalletTransactions.CreateAsync(walletTrans);
                    await _unitOfWork.SaveChangesAsync();
                }

                await _unitOfWork.SaveChangesAsync();

                return BaseResponse<object>.Success("Refund approved successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing refund decision");
                return BaseResponse<object>.Error("System error processing refund");
            }
        }
        public async Task<PagedResponse<List<RefundRequestResponse>>> GetRefundRequestsAsync(Guid userId, RefundRequestFilterRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                    return PagedResponse<List<RefundRequestResponse>>.Fail(new object(), "Access denied.", 401);

                var userRole = await _unitOfWork.UserRoles.FindAsync(x => x.UserID == userId);
                if (userRole == null) return PagedResponse<List<RefundRequestResponse>>.Fail(new object(), "User role not found.", 403);

                var role = await _unitOfWork.Roles.GetByIdAsync(userRole.RoleID);
                if (role == null || role.Name != "Admin")
                    return PagedResponse<List<RefundRequestResponse>>.Fail(new object(), "Permission denied. Admin access required.", 403);

                var query = _unitOfWork.RefundRequests.Query()
                    .Include(r => r.Student)
                    .Include(r => r.Purchase)
                        .ThenInclude(p => p.Course)
                    .Include(r => r.ProcessedByAdmin)
                    .AsQueryable();


                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var term = request.SearchTerm.ToLower().Trim();
                    query = query.Where(r =>
                        r.Student.FullName.ToLower().Contains(term) ||
                        r.Student.Email.ToLower().Contains(term) ||
                        (r.Purchase != null && r.Purchase.Course != null && r.Purchase.Course.Title.ToLower().Contains(term)) ||
                        r.RefundRequestID.ToString().Contains(term)
                    );
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<RefundRequestStatus>(request.Status, true, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }

                if (request.FromDate.HasValue)
                {
                    query = query.Where(r => r.RequestedAt >= request.FromDate.Value);
                }
                if (request.ToDate.HasValue)
                {
                    var toDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(r => r.RequestedAt <= toDate);
                }

                var totalItems = await query.CountAsync();

                var items = await query
                    .OrderByDescending(r => r.RequestedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(r => new RefundRequestResponse
                    {
                        RefundRequestId = r.RefundRequestID,
                        PurchaseId = r.PurchaseId ?? Guid.Empty,

                        StudentId = r.StudentID,
                        StudentName = r.Student.FullName ?? r.Student.UserName,
                        StudentEmail = r.Student.Email,
                        StudentAvatar = r.Student.Avatar,

                        CourseName = r.Purchase != null && r.Purchase.Course != null
                                     ? r.Purchase.Course.Title
                                     : "Unknown Course",
                        RefundAmount = r.RefundAmount,
                        OriginalAmount = r.Purchase != null ? r.Purchase.FinalAmount : 0,

                        RequestType = r.RequestType.ToString(),
                        Reason = r.Reason,
                        BankName = r.BankName,
                        BankAccountNumber = r.BankAccountNumber,
                        BankAccountHolderName = r.BankAccountHolderName,
                        ProofImageUrl = r.ProofImageUrl,

                        Status = r.Status.ToString(),
                        RequestedAt = r.RequestedAt.ToString("dd-MM-yyyy HH:mm"),
                        ProcessedAt = r.ProcessedAt.HasValue ? r.ProcessedAt.Value.ToString("dd-MM-yyyy HH:mm") : null,
                        AdminNote = r.AdminNote,
                        ProcessedByAdminName = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.FullName : null
                    })
                    .ToListAsync();

                return PagedResponse<List<RefundRequestResponse>>.Success(
                    items,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "Refund requests retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund requests for admin {AdminId}", userId);
                return PagedResponse<List<RefundRequestResponse>>.Error("System error while retrieving refund requests");
            }
        }
        public async Task<BaseResponse<RefundRequestResponse>> GetMyRefundRequestDetailAsync(Guid userId, Guid purchaseId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null)
                {
                    return BaseResponse<RefundRequestResponse>.Fail(new object(), "Access denied. User not found.", 401);
                }

                var refundRequest = await _unitOfWork.RefundRequests.Query()
                    .Include(r => r.Purchase)
                        .ThenInclude(p => p.Course)
                    .Include(r => r.ProcessedByAdmin)
                    .FirstOrDefaultAsync(r => r.PurchaseId == purchaseId && r.StudentID == userId);

                if (refundRequest == null)
                {
                    var purchaseExists = await _unitOfWork.Purchases.Query()
                        .AnyAsync(p => p.PurchasesId == purchaseId && p.UserId == userId);

                    if (!purchaseExists)
                    {
                        return BaseResponse<RefundRequestResponse>.Fail(new object(), "Purchase not found.", 404);
                    }

                    return BaseResponse<RefundRequestResponse>.Fail(new object(), "No refund request found for this purchase.", 404);
                }

                var response = new RefundRequestResponse
                {
                    RefundRequestId = refundRequest.RefundRequestID,
                    PurchaseId = refundRequest.PurchaseId ?? Guid.Empty,
                    StudentId = refundRequest.StudentID,
                    StudentName = user.FullName ?? user.UserName,
                    StudentEmail = user.Email,
                    StudentAvatar = user.Avatar,
                    CourseName = refundRequest.Purchase?.Course?.Title ?? "Unknown Course",
                    RefundAmount = refundRequest.RefundAmount,
                    OriginalAmount = refundRequest.Purchase?.FinalAmount ?? 0,
                    RequestType = refundRequest.RequestType.ToString(),
                    Reason = refundRequest.Reason,
                    BankName = refundRequest.BankName,
                    BankAccountNumber = refundRequest.BankAccountNumber,
                    BankAccountHolderName = refundRequest.BankAccountHolderName,
                    ProofImageUrl = refundRequest.ProofImageUrl,
                    Status = refundRequest.Status.ToString(),
                    RequestedAt = refundRequest.RequestedAt.ToString("dd-MM-yyyy HH:mm"),
                    ProcessedAt = refundRequest.ProcessedAt?.ToString("dd-MM-yyyy HH:mm"),

                    AdminNote = refundRequest.AdminNote,

                    ProcessedByAdminName = refundRequest.ProcessedByAdmin?.FullName ?? "Admin"
                };

                return BaseResponse<RefundRequestResponse>.Success(response, "Refund request details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving refund request detail for user {UserId}, purchase {PurchaseId}", userId, purchaseId);
                return BaseResponse<RefundRequestResponse>.Error("System error while retrieving refund details");
            }
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

                    var course = await _unitOfWork.Courses.Query()
                                    .Include(c => c.Teacher)
                                    .FirstOrDefaultAsync(c => c.CourseID == request.CourseId);

                    if (course == null || course.Status != CourseStatus.Published)
                        return BaseResponse<PurchaseCourseResponse>.Fail(new object(), "Course unavailable", 400);

                    if (course.Teacher != null && course.Teacher.UserId == userId)
                    {
                        return BaseResponse<PurchaseCourseResponse>.Fail(new object(), "You cannot purchase your own course.", 400);
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
                    EligibleForRefundUntil = purchaseEntity.EligibleForRefundUntil?.ToString("dd-MM-yyyy HH:mm"),
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
        public async Task<PagedResponse<List<RefundRequestResponse>>> GetStudentRefundRequestsByLanguageAsync(Guid userId, RefundRequestFilterRequest request)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return PagedResponse<List<RefundRequestResponse>>.Fail(new object(), "Access denied", 403);
                }

                if (user.ActiveLanguageId == null)
                {
                    return PagedResponse<List<RefundRequestResponse>>.Fail(new object(), "User has no active language set", 400);
                }

                var query = _unitOfWork.RefundRequests.Query()
                    .Include(r => r.Purchase)
                        .ThenInclude(p => p.Course)
                    .Include(r => r.ProcessedByAdmin)
                    .Where(r => r.StudentID == userId);

                query = query.Where(r => r.Purchase != null
                                      && r.Purchase.Course != null
                                      && r.Purchase.Course.LanguageId == user.ActiveLanguageId.Value);

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    var term = request.SearchTerm.ToLower().Trim();
                    query = query.Where(r =>
                        (r.Purchase != null && r.Purchase.Course != null && r.Purchase.Course.Title.ToLower().Contains(term)) ||
                        r.RefundRequestID.ToString().Contains(term)
                    );
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<RefundRequestStatus>(request.Status, true, out var statusEnum))
                {
                    query = query.Where(r => r.Status == statusEnum);
                }

                if (request.FromDate.HasValue)
                {
                    query = query.Where(r => r.RequestedAt >= request.FromDate.Value);
                }

                if (request.ToDate.HasValue)
                {
                    var toDate = request.ToDate.Value.Date.AddDays(1).AddTicks(-1);
                    query = query.Where(r => r.RequestedAt <= toDate);
                }

                var totalItems = await query.CountAsync();

                var items = await query
                    .OrderByDescending(r => r.RequestedAt)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(r => new RefundRequestResponse
                    {
                        RefundRequestId = r.RefundRequestID,
                        PurchaseId = r.PurchaseId ?? Guid.Empty,
                        StudentId = r.StudentID,
                        StudentName = user.FullName ?? user.UserName,
                        StudentEmail = user.Email,
                        StudentAvatar = user.Avatar,
                        CourseName = r.Purchase != null && r.Purchase.Course != null
                                     ? r.Purchase.Course.Title
                                     : "Unknown Course",
                        RefundAmount = r.RefundAmount,
                        OriginalAmount = r.Purchase != null ? r.Purchase.FinalAmount : 0,
                        RequestType = r.RequestType.ToString(),
                        Reason = r.Reason ?? "Unknown",
                        BankName = r.BankName ?? "Unknown",
                        BankAccountNumber = r.BankAccountNumber ?? "Unknown",
                        BankAccountHolderName = r.BankAccountHolderName ?? "Unknown",
                        ProofImageUrl = r.ProofImageUrl ?? "Unknown",
                        Status = r.Status.ToString(),
                        RequestedAt = r.RequestedAt.ToString("dd-MM-yyyy HH:mm"),
                        ProcessedAt = r.ProcessedAt.HasValue ? r.ProcessedAt.Value.ToString("dd-MM-yyyy HH:mm") : null,
                        AdminNote = r.AdminNote,
                        ProcessedByAdminName = r.ProcessedByAdmin != null ? r.ProcessedByAdmin.UserName : null
                    })
                    .ToListAsync();

                return PagedResponse<List<RefundRequestResponse>>.Success(
                    items,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "My refund requests retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund requests for student {UserId}", userId);
                return PagedResponse<List<RefundRequestResponse>>.Error("System error while retrieving refund requests");
            }
        }
        public async Task<BaseResponse<RefundRequestResponse>> GetRefundRequestDetailByIdAsync(Guid userId, Guid refundRequestId)
        {
            try
            {
                var user = await _unitOfWork.Users.GetByIdAsync(userId);
                if (user == null || !user.IsEmailConfirmed || !user.Status)
                {
                    return BaseResponse<RefundRequestResponse>.Fail(new object(), "Access denied", 403);
                }

                var refundRequest = await _unitOfWork.RefundRequests.Query()
                    .Include(r => r.Purchase)
                        .ThenInclude(p => p.Course)
                    .Include(r => r.ProcessedByAdmin)
                    .FirstOrDefaultAsync(r => r.RefundRequestID == refundRequestId);

                if (refundRequest == null)
                {
                    return BaseResponse<RefundRequestResponse>.Fail(new object(), "Refund request not found", 404);
                }

                if (refundRequest.StudentID != userId)
                {
                    return BaseResponse<RefundRequestResponse>.Fail(new object(), "Refund request not found", 404);
                }

                var response = new RefundRequestResponse
                {
                    RefundRequestId = refundRequest.RefundRequestID,
                    PurchaseId = refundRequest.PurchaseId ?? Guid.Empty,
                    StudentId = refundRequest.StudentID,
                    StudentName = user.FullName ?? user.UserName,
                    StudentEmail = user.Email,
                    StudentAvatar = user.Avatar,

                    CourseName = refundRequest.Purchase != null && refundRequest.Purchase.Course != null
                                 ? refundRequest.Purchase.Course.Title
                                 : "Unknown Course",

                    RefundAmount = refundRequest.RefundAmount,
                    OriginalAmount = refundRequest.Purchase != null ? refundRequest.Purchase.FinalAmount : 0,

                    RequestType = refundRequest.RequestType.ToString(),
                    Reason = refundRequest.Reason ?? "Unknown",
                    BankName = refundRequest.BankName ?? "Unknown",
                    BankAccountNumber = refundRequest.BankAccountNumber ?? "Unknown",
                    BankAccountHolderName = refundRequest.BankAccountHolderName ?? "Unknown",
                    ProofImageUrl = refundRequest.ProofImageUrl ?? "Unknown",
                    Status = refundRequest.Status.ToString(),
                    RequestedAt = refundRequest.RequestedAt.ToString("dd-MM-yyyy HH:mm"),
                    ProcessedAt = refundRequest.ProcessedAt?.ToString("dd-MM-yyyy HH:mm"),
                    AdminNote = refundRequest.AdminNote,
                    ProcessedByAdminName = refundRequest.ProcessedByAdmin != null ? refundRequest.ProcessedByAdmin.UserName : null
                };

                return BaseResponse<RefundRequestResponse>.Success(response, "Refund request details retrieved successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting refund detail by id {RefundRequestId} for user {UserId}", refundRequestId, userId);
                return BaseResponse<RefundRequestResponse>.Error("System error while retrieving refund details");
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
        private async Task<PagedResponse<List<CoursePurchaseResponse>>> GetCoursePurchasesByLanguageInternalAsync(Guid userId, Guid languageId, int page, int pageSize, PurchaseStatus? status = null, bool? activeOnly = null)
        {
            var now = TimeHelper.GetVietnamTime();

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

            var purchasesList = await query
                .OrderByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

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
                EligibleForRefundUntil = p.EligibleForRefundUntil?.ToString("dd-MM-yyyy HH:mm"),
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
                        return new CourseAccessResponse
                        {
                            HasAccess = true,
                            AccessStatus = "ENROLLED",
                            EnrollmentId = existingEnrollment.EnrollmentID
                        };
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
                            await _unitOfWork.Enrollments.UpdateAsync(enrollment);
                            await _unitOfWork.SaveChangesAsync();
                        }
                        return new CourseAccessResponse
                        {
                            HasAccess = false,
                            AccessStatus = "REFUNDED",
                            PurchaseId = latestPurchase.PurchasesId,
                            EnrollmentId = enrollment?.EnrollmentID
                        };

                    case PurchaseStatus.Expired:
                        if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Expired)
                        {
                            enrollment.Status = DAL.Type.EnrollmentStatus.Expired;
                            await _unitOfWork.Enrollments.UpdateAsync(enrollment);
                            await _unitOfWork.SaveChangesAsync();
                        }
                        return new CourseAccessResponse
                        {
                            HasAccess = false,
                            AccessStatus = "EXPIRED",
                            ExpiresAt = latestPurchase.ExpiresAt?.ToString("dd-MM-yyyy hh:MM"),
                            DaysRemaining = 0,
                            PurchaseId = latestPurchase.PurchasesId,
                            EnrollmentId = enrollment?.EnrollmentID
                        };

                    case PurchaseStatus.Failed:
                        return new CourseAccessResponse
                        {
                            HasAccess = false,
                            AccessStatus = "PAYMENT_FAILED",
                            PurchaseId = latestPurchase.PurchasesId,
                            EnrollmentId = enrollment?.EnrollmentID
                        };

                    case PurchaseStatus.Pending:
                        return new CourseAccessResponse
                        {
                            HasAccess = false,
                            AccessStatus = "PENDING_PAYMENT",
                            PurchaseId = latestPurchase.PurchasesId,
                            EnrollmentId = enrollment?.EnrollmentID
                        };
                }

                if (latestPurchase.ExpiresAt.HasValue && now > latestPurchase.ExpiresAt.Value)
                {
                    latestPurchase.Status = PurchaseStatus.Expired;

                    if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Expired)
                    {
                        enrollment.Status = DAL.Type.EnrollmentStatus.Expired;
                    }

                    await _unitOfWork.Purchases.UpdateAsync(latestPurchase);
                    if (enrollment != null)
                    {
                        await _unitOfWork.Enrollments.UpdateAsync(enrollment);
                    }

                    await _unitOfWork.SaveChangesAsync();

                    return new CourseAccessResponse
                    {
                        HasAccess = false,
                        AccessStatus = "EXPIRED",
                        ExpiresAt = latestPurchase.ExpiresAt.Value.ToString("dd-MM-yyyy hh:MM"),
                        DaysRemaining = 0,
                        PurchaseId = latestPurchase.PurchasesId,
                        EnrollmentId = enrollment?.EnrollmentID
                    };
                }

                if (enrollment != null && enrollment.Status != DAL.Type.EnrollmentStatus.Active)
                {
                    enrollment.Status = DAL.Type.EnrollmentStatus.Active;
                    await _unitOfWork.Enrollments.UpdateAsync(enrollment);
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
                    ExpiresAt = latestPurchase.ExpiresAt?.ToString("dd-MM-yyyy hh:MM"),
                    DaysRemaining = daysRemaining,
                    PurchaseId = latestPurchase.PurchasesId,
                    RefundEligibleUntil = latestPurchase.EligibleForRefundUntil?.ToString("dd-MM-yyyy hh:MM"),
                    EnrollmentId = enrollment?.EnrollmentID
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
