using BLL.IServices.Statistics;
using Common.DTO.ApiResponse;
using Common.DTO.Statistics.Request;
using Common.DTO.Statistics.Response;
using DAL.Type;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Statistics
{
    public class StatisticsService : IStatisticsService
    {
        private readonly IUnitOfWork _unitOfWork;

        public StatisticsService(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task<BaseResponse<CourseRevenueYearlyResponse>> GetCourseRevenueStatsAsync(Guid userId, CourseStatisticRequest request)
        {
            try
            {
                // 1. Validate User & Course Ownership
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<CourseRevenueYearlyResponse>.Fail(null, "Access denied. User is not a teacher.", 403);

                var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
                if (course == null)
                    return BaseResponse<CourseRevenueYearlyResponse>.Fail(null, "Course not found.", 404);

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<CourseRevenueYearlyResponse>.Fail(null, "You do not have permission to view statistics for this course.", 403);

                // 2. Query Purchases
                // Lấy các giao dịch thành công (Completed), thuộc khóa học này, trong năm được yêu cầu
                var purchases = await _unitOfWork.Purchases.Query()
                    .Where(p => p.CourseId == request.CourseId
                             && p.Status == PurchaseStatus.Completed
                             && p.PaidAt.HasValue
                             && p.PaidAt.Value.Year == request.Year)
                    .Select(p => new { p.PaidAt, p.FinalAmount })
                    .ToListAsync();

                // 3. Process Data Grouping
                var monthlyStats = new List<RevenueStatResponse>();

                // Loop 1 -> 12 để đảm bảo trả về đủ 12 tháng kể cả tháng không có doanh thu
                for (int month = 1; month <= 12; month++)
                {
                    var dataInMonth = purchases.Where(p => p.PaidAt!.Value.Month == month).ToList();
                    monthlyStats.Add(new RevenueStatResponse
                    {
                        Month = month,
                        TransactionCount = dataInMonth.Count,
                        TotalRevenue = dataInMonth.Sum(x => x.FinalAmount)
                    });
                }

                var response = new CourseRevenueYearlyResponse
                {
                    CourseId = course.CourseID,
                    CourseTitle = course.Title,
                    Year = request.Year,
                    TotalYearlyRevenue = monthlyStats.Sum(x => x.TotalRevenue),
                    MonthlyBreakdown = monthlyStats
                };

                return BaseResponse<CourseRevenueYearlyResponse>.Success(response, "Revenue statistics retrieved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseRevenueYearlyResponse>.Error($"Error calculating revenue stats: {ex.Message}");
            }
        }

        public async Task<BaseResponse<CourseEnrollmentYearlyResponse>> GetCourseEnrollmentStatsAsync(Guid userId, CourseStatisticRequest request)
        {
            try
            {
                // 1. Validate User & Course Ownership
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<CourseEnrollmentYearlyResponse>.Fail(null, "Access denied.", 403);

                var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
                if (course == null) return BaseResponse<CourseEnrollmentYearlyResponse>.Fail(null, "Course not found.", 404);

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<CourseEnrollmentYearlyResponse>.Fail(null, "Access denied.", 403);

                // 2. Query Enrollments
                // Lấy enrollment trong năm, loại bỏ các trạng thái Cancelled
                var enrollments = await _unitOfWork.Enrollments.Query()
                    .Where(e => e.CourseId == request.CourseId
                             && e.Status != DAL.Type.EnrollmentStatus.Cancelled
                             && e.EnrolledAt.Year == request.Year)
                    .Select(e => new { e.EnrolledAt })
                    .ToListAsync();

                // 3. Process Data
                var monthlyStats = new List<EnrollmentStatResponse>();
                for (int month = 1; month <= 12; month++)
                {
                    int count = enrollments.Count(e => e.EnrolledAt.Month == month);
                    monthlyStats.Add(new EnrollmentStatResponse
                    {
                        Month = month,
                        NewEnrollments = count
                    });
                }

                var response = new CourseEnrollmentYearlyResponse
                {
                    CourseId = course.CourseID,
                    CourseTitle = course.Title,
                    Year = request.Year,
                    TotalYearlyEnrollments = monthlyStats.Sum(x => x.NewEnrollments),
                    MonthlyBreakdown = monthlyStats
                };

                return BaseResponse<CourseEnrollmentYearlyResponse>.Success(response, "Enrollment statistics retrieved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseEnrollmentYearlyResponse>.Error($"Error calculating enrollment stats: {ex.Message}");
            }
        }

        public async Task<BaseResponse<CourseReviewStatResponse>> GetCourseReviewAnalysisAsync(Guid userId, Guid courseId)
        {
            try
            {
                // 1. Validate User & Course Ownership
                // Lưu ý: Có thể cho phép Manager xem cái này, ở đây tôi đang check Teacher
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null)
                    return BaseResponse<CourseReviewStatResponse>.Fail(null, "Access denied.", 403);

                var course = await _unitOfWork.Courses.GetByIdAsync(courseId);
                if (course == null) return BaseResponse<CourseReviewStatResponse>.Fail(null, "Course not found.", 404);

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<CourseReviewStatResponse>.Fail(null, "Access denied.", 403);

                // 2. Query Reviews
                var reviews = await _unitOfWork.CourseReviews.Query()
                    .Where(r => r.CourseId == courseId)
                    .Select(r => new { r.Rating })
                    .ToListAsync();

                // 3. Calculate Stats
                int totalReviews = reviews.Count;
                double averageRating = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0;

                var starDistribution = new List<StarDistribution>();

                // Loop từ 5 sao xuống 1 sao
                for (int star = 5; star >= 1; star--)
                {
                    int count = reviews.Count(r => r.Rating == star);
                    double percentage = totalReviews > 0 ? Math.Round((double)count / totalReviews * 100, 1) : 0;

                    starDistribution.Add(new StarDistribution
                    {
                        Star = star,
                        Count = count,
                        Percentage = percentage
                    });
                }

                var response = new CourseReviewStatResponse
                {
                    CourseId = course.CourseID,
                    CourseTitle = course.Title,
                    TotalReviews = totalReviews,
                    AverageRating = Math.Round(averageRating, 1),
                    StarDistribution = starDistribution
                };

                return BaseResponse<CourseReviewStatResponse>.Success(response, "Review analysis retrieved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<CourseReviewStatResponse>.Error($"Error analyzing reviews: {ex.Message}");
            }
        }
        public async Task<BaseResponse<PagedReviewResponse>> GetCourseReviewDetailsAsync(Guid userId, ReviewDetailRequest request)
        {
            try
            {
                var teacher = await _unitOfWork.TeacherProfiles.FindAsync(t => t.UserId == userId);
                if (teacher == null) return BaseResponse<PagedReviewResponse>.Fail(null, "Access denied.", 403);

                var course = await _unitOfWork.Courses.GetByIdAsync(request.CourseId);
                if (course == null) return BaseResponse<PagedReviewResponse>.Fail(null, "Course not found.", 404);

                if (course.TeacherId != teacher.TeacherId)
                    return BaseResponse<PagedReviewResponse>.Fail(null, "Access denied.", 403);

                // 2. Query
                var query = _unitOfWork.CourseReviews.Query()
                    .AsNoTracking()
                    .Where(r => r.CourseId == request.CourseId);

                // Filter theo sao nếu có
                if (request.Rating.HasValue && request.Rating > 0)
                {
                    query = query.Where(r => r.Rating == request.Rating.Value);
                }

                // Include thông tin User để lấy tên và avatar
                // Giả sử quan hệ: Review -> Learner -> User
                var queryWithInfo = query
                    .Include(r => r.Learner)
                    .ThenInclude(l => l.User);

                // Đếm tổng số lượng trước khi phân trang
                var totalItems = await queryWithInfo.CountAsync();
                var totalPages = (int)Math.Ceiling(totalItems / (double)request.PageSize);

                // Phân trang và Select
                var reviews = await queryWithInfo
                    .OrderByDescending(r => r.CreatedAt) // Mới nhất lên đầu
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(r => new ReviewDetailItem
                    {
                        ReviewId = r.CourseReviewId,
                        LearnerName = r.Learner.User.FullName ?? r.Learner.User.UserName,
                        LearnerAvatar = r.Learner.User.Avatar,
                        Rating = r.Rating,
                        Comment = r.Comment,
                        CreatedAt = r.CreatedAt
                    })
                    .ToListAsync();

                var response = new PagedReviewResponse
                {
                    CurrentPage = request.Page,
                    TotalItems = totalItems,
                    TotalPages = totalPages,
                    Reviews = reviews
                };

                return BaseResponse<PagedReviewResponse>.Success(response, "Review details retrieved successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<PagedReviewResponse>.Error($"Error fetching reviews: {ex.Message}");
            }
        }
    }
}
