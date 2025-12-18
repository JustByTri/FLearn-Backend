using Common.DTO.ApiResponse;
using Common.DTO.Statistics.Request;
using Common.DTO.Statistics.Response;

namespace BLL.IServices.Statistics
{
    public interface IStatisticsService
    {
        /// <summary>
        /// Tính doanh thu của khóa học theo từng tháng trong năm
        /// </summary>
        Task<BaseResponse<CourseRevenueYearlyResponse>> GetCourseRevenueStatsAsync(Guid userId, CourseStatisticRequest request);

        /// <summary>
        /// Thống kê số lượng học viên đăng ký mới theo từng tháng trong năm
        /// </summary>
        Task<BaseResponse<CourseEnrollmentYearlyResponse>> GetCourseEnrollmentStatsAsync(Guid userId, CourseStatisticRequest request);

        /// <summary>
        /// Phân tích đánh giá khóa học (phân bố sao, điểm trung bình)
        /// </summary>
        Task<BaseResponse<CourseReviewStatResponse>> GetCourseReviewAnalysisAsync(Guid userId, Guid courseId);
        Task<BaseResponse<PagedReviewResponse>> GetCourseReviewDetailsAsync(Guid userId, ReviewDetailRequest request);
    }
}
