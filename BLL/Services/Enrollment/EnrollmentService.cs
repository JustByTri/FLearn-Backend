using BLL.IServices.Enrollment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Enrollment.Request;
using Common.DTO.Enrollment.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.Helpers;
using DAL.Models;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Enrollment
{
    public class EnrollmentService : IEnrollmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPurchaseService _purchaseService;
        public EnrollmentService(IUnitOfWork unitOfWork, IPurchaseService purchaseService)
        {
            _unitOfWork = unitOfWork;
            _purchaseService = purchaseService;
        }
        public async Task<BaseResponse<EnrollmentResponse>> EnrolCourseAsync(Guid userId, EnrollmentRequest request)
        {
            var strategy = _unitOfWork.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await _unitOfWork.BeginTransactionAsync();
                try
                {
                    var user = await _unitOfWork.Users.GetByIdAsync(userId);
                    if (user == null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Unauthorized", 401);

                    if (!user.Status)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Account is inactive", 403);

                    if (!user.IsEmailConfirmed)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Email not confirmed", 403);

                    var learner = await _unitOfWork.LearnerLanguages.FindAsync(l => l.UserId == userId);
                    if (learner == null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Unauthorized", 401);

                    var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);

                    if (course == null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "Course not found.", 404);

                    if (course.Status != CourseStatus.Published)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "This course is not yet published and cannot be enrolled.", 400);

                    if (course.CourseType == CourseType.Paid)
                    {
                        bool hasPurchased = await _unitOfWork.Courses.HasUserPurchasedCourseAsync(userId, request.CourseId);
                        if (!hasPurchased)
                            return BaseResponse<EnrollmentResponse>.Fail(new object(), "Course not purchased", 403);
                    }

                    var existingEnrollment = await _unitOfWork.Enrollments.Query()
                        .FirstOrDefaultAsync(e => e.CourseId == request.CourseId && e.LearnerId == learner.LearnerLanguageId);


                    if (existingEnrollment != null)
                        return BaseResponse<EnrollmentResponse>.Fail(new object(), "You are already enrolled in this course.", 400);

                    var response = await _purchaseService.CheckCourseAccessAsync(userId, course.CourseID);

                    var enrollment = new DAL.Models.Enrollment
                    {
                        EnrollmentID = Guid.NewGuid(),
                        CourseId = request.CourseId,
                        LearnerId = learner.LearnerLanguageId,
                        EnrolledAt = TimeHelper.GetVietnamTime()
                    };

                    var purchase = new Purchase();
                    if (response.Data != null && response.Data.PurchaseId != null)
                    {
                        purchase = await _unitOfWork.Purchases.GetByIdAsync((Guid)response.Data.PurchaseId);
                        if (purchase != null)
                        {
                            purchase.EnrollmentId = enrollment.EnrollmentID;
                            await _unitOfWork.Purchases.UpdateAsync(purchase);
                        }
                    }

                    await _unitOfWork.Enrollments.CreateAsync(enrollment);
                    await _unitOfWork.SaveChangesAsync();
                    await _unitOfWork.CommitTransactionAsync();

                    var enrollmentResponse = new EnrollmentResponse
                    {
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseId = course.CourseID,
                        CourseType = course.CourseType.ToString(),
                        AccessUntil = response.Data?.ExpiresAt,
                        EligibleForRefundUntil = response.Data?.RefundEligibleUntil,
                        CourseTitle = course.Title,
                        PricePaid = purchase?.FinalAmount ?? 0,
                        Status = enrollment.Status.ToString(),
                        ProgressPercent = enrollment.ProgressPercent,
                        EnrollmentDate = enrollment.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                    };

                    return BaseResponse<EnrollmentResponse>.Success(enrollmentResponse, "Enrolled successfully.");
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync();
                    return BaseResponse<EnrollmentResponse>.Error($"Unexpected error occurred: {ex.Message}");
                }
            });
        }
        public async Task<PagedResponse<IEnumerable<EnrollmentResponse>>> GetEnrolledCoursesAsync(Guid userId, string lang, PagingRequest request)
        {
            try
            {
                var learner = await _unitOfWork.LearnerLanguages
                    .Query()
                    .FirstOrDefaultAsync(l => l.UserId == userId);

                if (learner == null)
                {
                    return PagedResponse<IEnumerable<EnrollmentResponse>>.Fail(
                        new object(),
                        "Access denied",
                        403
                    );
                }

                var query = _unitOfWork.Enrollments
                    .Query()
                    .Where(e => e.LearnerId == learner.LearnerLanguageId)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Template)
                    .Include(e => e.Course)
                        .ThenInclude(c => c.Language)
                    .Include(e => e.Purchases)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(e =>
                        e.Course.Title.Contains(request.SearchTerm) ||
                        e.Course.Description.Contains(request.SearchTerm));
                }

                if (!string.IsNullOrEmpty(request.Status) && Enum.TryParse<DAL.Type.EnrollmentStatus>(request.Status, out var statusFilter))
                {
                    query = query.Where(e => e.Status == statusFilter);
                }

                var totalCount = await query.CountAsync();

                query = request.SortBy?.ToLower() switch
                {
                    "title" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.Course.Title)
                        : query.OrderBy(e => e.Course.Title),
                    "progress" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.ProgressPercent)
                        : query.OrderBy(e => e.ProgressPercent),
                    "enrolledat" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.EnrolledAt)
                        : query.OrderBy(e => e.EnrolledAt),
                    "lastaccessed" => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.LastAccessedAt)
                        : query.OrderBy(e => e.LastAccessedAt),
                    _ => request.SortBy?.ToLower() == "desc"
                        ? query.OrderByDescending(e => e.EnrolledAt)
                        : query.OrderBy(e => e.EnrolledAt)
                };

                var enrollments = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .ToListAsync();

                // Map to response DTOs - FIXED PURCHASE LOGIC
                var enrollmentResponses = new List<EnrollmentResponse>();

                foreach (var enrollment in enrollments)
                {
                    var purchase = await _unitOfWork.Purchases
                        .Query()
                        .FirstOrDefaultAsync(p =>
                            p.EnrollmentId == enrollment.EnrollmentID &&
                            p.Status == PurchaseStatus.Completed);

                    if (purchase == null && enrollment.Purchases?.Any() == true)
                    {
                        purchase = enrollment.Purchases
                            .FirstOrDefault(p => p.Status == PurchaseStatus.Completed);
                    }
                    if (purchase == null)
                    {
                        purchase = await _unitOfWork.Purchases
                            .Query()
                            .FirstOrDefaultAsync(p =>
                                p.UserId == userId &&
                                p.CourseId == enrollment.CourseId &&
                                p.Status == PurchaseStatus.Completed);
                    }

                    var courseAccess = await _purchaseService.CheckCourseAccessAsync(userId, enrollment.CourseId);
                    var response = new EnrollmentResponse
                    {
                        EnrollmentId = enrollment.EnrollmentID,
                        CourseId = enrollment.CourseId,
                        CourseTitle = enrollment.Course?.Title,
                        CourseType = enrollment.Course?.CourseType.ToString(),
                        PricePaid = purchase?.FinalAmount ?? 0,
                        ProgressPercent = enrollment.ProgressPercent,
                        EnrollmentDate = enrollment.EnrolledAt.ToString("dd-MM-yyyy HH:mm"),
                        AccessUntil = purchase?.ExpiresAt?.ToString("dd-MM-yyyy HH:mm"),
                        EligibleForRefundUntil = purchase?.EligibleForRefundUntil?.ToString("dd-MM-yyyy HH:mm"),
                        LastAccessedAt = enrollment.LastAccessedAt?.ToString("dd-MM-yyyy HH:mm"),
                        Status = enrollment.Status.ToString()
                    };

                    enrollmentResponses.Add(response);
                }

                return PagedResponse<IEnumerable<EnrollmentResponse>>.Success(
                    enrollmentResponses,
                    request.Page,
                    request.PageSize,
                    totalCount,
                    "Enrolled courses retrieved successfully"
                );
            }
            catch (Exception ex)
            {
                return PagedResponse<IEnumerable<EnrollmentResponse>>.Error(
                    $"Unexpected error occurred: {ex.Message}"
                );
            }
        }
    }
}
