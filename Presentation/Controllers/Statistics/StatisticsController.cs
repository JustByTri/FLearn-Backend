using BLL.IServices.Statistics;
using Common.DTO.Statistics.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Statistics
{
    [Route("api/statistics")]
    [ApiController]
    public class StatisticsController : ControllerBase
    {
        private readonly IStatisticsService _statisticsService;

        public StatisticsController(IStatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }
        /// <summary>
        /// Lấy thống kê doanh thu của một khóa học theo năm
        /// API: GET api/statistics/revenue?courseId=...&year=2025
        /// </summary>
        [HttpGet("revenue")]
        [Authorize]
        public async Task<IActionResult> GetCourseRevenue([FromQuery] CourseStatisticRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _statisticsService.GetCourseRevenueStatsAsync(userId, request);

            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Lấy thống kê số lượng học viên enroll theo năm
        /// API: GET api/statistics/enrollments?courseId=...&year=2025
        /// </summary>
        [HttpGet("enrollments")]
        [Authorize]
        public async Task<IActionResult> GetCourseEnrollments([FromQuery] CourseStatisticRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _statisticsService.GetCourseEnrollmentStatsAsync(userId, request);

            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Lấy phân tích đánh giá (Review analytics) của khóa học
        /// API: GET api/statistics/reviews/{courseId}
        /// </summary>
        [HttpGet("reviews/{courseId}")]
        [Authorize]
        public async Task<IActionResult> GetCourseReviewAnalysis(Guid courseId)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _statisticsService.GetCourseReviewAnalysisAsync(userId, courseId);

            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Lấy danh sách chi tiết bình luận có phân trang
        /// API: GET api/statistics/reviews-details?CourseId=...&Page=1&Rating=5
        /// </summary>
        [HttpGet("reviews-details")]
        [Authorize]
        public async Task<IActionResult> GetReviewDetails([FromQuery] ReviewDetailRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var result = await _statisticsService.GetCourseReviewDetailsAsync(userId, request);

            return StatusCode(result.Code, result);
        }
    }
}
