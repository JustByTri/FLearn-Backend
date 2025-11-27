using BLL.IServices.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Manager
{
    [Route("api/manager/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin, Manager")]
    public class ManagerDashboardController : ControllerBase
    {
        private readonly IManagerDashboardService _dashboardService;

        public ManagerDashboardController(IManagerDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }
        /// <summary>
        /// Lấy tổng quan KPI (User, Doanh thu, Churn Rate...)
        /// Default: 30 ngày gần nhất nếu không truyền tham số
        /// </summary>
        [HttpGet("overview")]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            // Xử lý default date: Nếu ko truyền thì lấy 30 ngày qua
            var end = endDate ?? DateTime.Now;
            var start = startDate ?? DateTime.Now.AddDays(-30);

            if (start > end)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var result = await _dashboardService.GetKpiOverviewAsync(start, end);
            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Lấy số liệu về mức độ tương tác (Thời gian học, tỷ lệ hoàn thành)
        /// </summary>
        [HttpGet("engagement")]
        public async Task<IActionResult> GetEngagementMetrics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var end = endDate ?? DateTime.Now;
            var start = startDate ?? DateTime.Now.AddDays(-30);

            if (start > end)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var result = await _dashboardService.GetEngagementMetricsAsync(start, end);
            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Phân tích hiệu quả nội dung (Tìm bài học có tỷ lệ bỏ cuộc cao nhất)
        /// </summary>
        /// <param name="top">Số lượng bản ghi muốn lấy (Default: 10)</param>
        [HttpGet("content-effectiveness")]
        public async Task<IActionResult> GetContentEffectiveness([FromQuery] int top = 10)
        {
            if (top <= 0) top = 10;
            if (top > 50) top = 50;

            var result = await _dashboardService.GetContentEffectivenessAsync(top);
            return StatusCode(result.Code, result);
        }
    }
}
