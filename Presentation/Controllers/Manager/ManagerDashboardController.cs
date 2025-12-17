using BLL.IServices.Admin;
using BLL.IServices.Dashboard;
using Common.DTO.Admin;
using DAL.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.Security.Claims;

namespace Presentation.Controllers.Manager
{
    [Route("api/manager/dashboard")]
    [ApiController]
    [Authorize(Roles = "Admin, Manager")]
    public class ManagerDashboardController : ControllerBase
    {
        private readonly IManagerDashboardService _dashboardService;
        private readonly IClassAdminService _classAdminService;
        private readonly ILogger<ManagerDashboardController> _logger;
        public ManagerDashboardController(IManagerDashboardService dashboardService, IClassAdminService classAdminService, ILogger<ManagerDashboardController> logger)
        {
            _classAdminService = classAdminService;
            _dashboardService = dashboardService;
            _logger = logger;
        }
        /// <summary>
        /// Lấy tổng quan KPI (User, Doanh thu, Churn Rate...)
        /// Default: 30 ngày gần nhất nếu không truyền tham số
        /// </summary>
        [HttpGet("overview")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetOverview([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var end = endDate ?? TimeHelper.GetVietnamTime();
            var start = startDate ?? TimeHelper.GetVietnamTime().AddDays(-30);

            if (start > end)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var result = await _dashboardService.GetKpiOverviewAsync(userId, start, end);
            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Lấy số liệu về mức độ tương tác (Thời gian học, tỷ lệ hoàn thành)
        /// </summary>
        [HttpGet("engagement")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetEngagementMetrics([FromQuery] DateTime? startDate, [FromQuery] DateTime? endDate)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var end = endDate ?? TimeHelper.GetVietnamTime();
            var start = startDate ?? TimeHelper.GetVietnamTime().AddDays(-30);

            if (start > end)
            {
                return BadRequest(new { message = "Start date must be before end date" });
            }

            var result = await _dashboardService.GetEngagementMetricsAsync(userId, start, end);
            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Phân tích hiệu quả nội dung (Tìm bài học có tỷ lệ bỏ cuộc cao nhất)
        /// </summary>
        /// <param name="top">Số lượng bản ghi muốn lấy (Default: 10)</param>
        [HttpGet("content-effectiveness")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetContentEffectiveness([FromQuery] int top = 10)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (top <= 0) top = 10;
            if (top > 50) top = 50;

            var result = await _dashboardService.GetContentEffectivenessAsync(userId, top);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// [Manager] Lấy danh sách yêu cầu hủy lớp đang chờ duyệt
        /// </summary>
        [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<ClassCancellationRequestDto>), 200)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 403)]
        public async Task<IActionResult> GetPendingRequests()
        {
            try
            {
                if (!this.TryGetUserId(out var managerId, out var error))
                    return error!;

                var requests = await _classAdminService.GetPendingCancellationRequestsAsync(managerId);

                return Ok(new
                {
                    success = true,
                    data = requests,
                    count = requests.Count()
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting pending cancellation requests");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Manager] Xem chi tiết một yêu cầu hủy lớp
        /// </summary>
        [HttpGet("{requestId:guid}")]
        [ProducesResponseType(typeof(ClassCancellationRequestDto), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetRequestById(Guid requestId)
        {
            try
            {
                if (!this.TryGetUserId(out var managerId, out var error))
                    return error!;

                var request = await _classAdminService.GetCancellationRequestByIdAsync(managerId, requestId);

                return Ok(new
                {
                    success = true,
                    data = request
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cancellation request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Manager] Duyệt yêu cầu hủy lớp
        /// </summary>
        /// <param name="requestId">ID của yêu cầu cần duyệt</param>
        /// <param name="dto">Ghi chú từ Manager (optional)</param>
        [HttpPost("{requestId:guid}/approve")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> ApproveRequest(
            Guid requestId,
            [FromBody] ApproveCancellationRequestDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var managerId, out var error))
                    return error!;

                var result = await _classAdminService.ApproveCancellationRequestAsync(
                    managerId,
                    requestId,
                    dto.Note);

                return Ok(new
                {
                    success = true,
                    message = "Đã duyệt yêu cầu hủy lớp. Hệ thống sẽ tự động tạo đơn hoàn tiền cho học viên."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error approving cancellation request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Manager] Từ chối yêu cầu hủy lớp
        /// </summary>
        /// <param name="requestId">ID của yêu cầu cần từ chối</param>
        /// <param name="dto">Lý do từ chối (required)</param>
        [HttpPost("{requestId:guid}/reject")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> RejectRequest(
            Guid requestId,
            [FromBody] RejectCancellationRequestDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var managerId, out var error))
                    return error!;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _classAdminService.RejectCancellationRequestAsync(
                    managerId,
                    requestId,
                    dto.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Đã từ chối yêu cầu hủy lớp. Giáo viên sẽ nhận được thông báo."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting cancellation request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }
    }
}

