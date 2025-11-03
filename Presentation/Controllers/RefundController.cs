using BLL.IServices.Refund;
using Common.DTO.Refund;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class RefundController : ControllerBase
    {
        private readonly IRefundRequestService _refundRequestService;
        private readonly ILogger<RefundController> _logger;

        public RefundController(IRefundRequestService refundRequestService, ILogger<RefundController> logger)
        {
            _refundRequestService = refundRequestService;
            _logger = logger;
        }

        // ================== ADMIN ACTIONS ==================

        /// <summary>
        /// [Admin] Bước 1: Gửi email thông báo học viên cần làm đơn hoàn tiền
        /// </summary>
        [HttpPost("admin/notify-student")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> NotifyStudentToCreateRefund([FromBody] NotifyRefundRequestDto dto)
        {
            try
            {
                await _refundRequestService.NotifyStudentToCreateRefundAsync(dto);
                return Ok(new { success = true, message = "Đã gửi email thông báo thành công." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi thông báo hoàn tiền");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        /// <summary>
        /// [Admin] Bước 3: Xem tất cả các đơn hoàn tiền (có thể lọc theo trạng thái)
        /// </summary>
        [HttpGet("admin/list")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRefundRequests([FromQuery] RefundRequestStatus? status)
        {
            try
            {
                var requests = await _refundRequestService.GetRefundRequestsAsync(status);
                return Ok(new { success = true, data = requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi Admin lấy danh sách RefundRequest");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        /// <summary>
        /// [Admin] Bước 3.1: Xem chi tiết một đơn hoàn tiền
        /// </summary>
        [HttpGet("admin/{refundRequestId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetRefundRequestById(Guid refundRequestId)
        {
            try
            {
                var request = await _refundRequestService.GetRefundRequestByIdAsync(refundRequestId);
                return Ok(new { success = true, data = request });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lấy chi tiết RefundRequest {Id}", refundRequestId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        /// <summary>
        /// [Admin] Bước 4: Xử lý đơn hoàn tiền (Approve hoặc Reject) và gửi email kết quả
        /// - Approve: Gửi kèm hình ảnh chứng minh đã chuyển khoản
        /// - Reject: Gửi kèm lý do từ chối
        /// </summary>
        [HttpPost("admin/process")]
        [Authorize(Roles = "Admin")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ProcessRefundRequest([FromForm] ProcessRefundRequestDto dto)
        {
            try
            {
                var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _refundRequestService.ProcessRefundRequestAsync(dto, adminId);

                var actionText = dto.Action == ProcessAction.Approve ? "chấp nhận" : "từ chối";
                return Ok(new
                {
                    success = true,
                    message = $"Đã {actionText} đơn hoàn tiền và gửi email thông báo thành công.",
                    data = result
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
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý RefundRequest");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        // ================== LEARNER (HỌC VIÊN) ACTIONS ==================

        /// <summary>
        /// [Học viên] Bước 2: Gửi đơn yêu cầu hoàn tiền
        /// 
        /// Hướng dẫn:
        /// 1. Vào trang cá nhân
        /// 2. Nhấn "Gửi đơn hoàn tiền"
        /// 3. Chọn lý do hoàn tiền (Lớp bị hủy, Lý do cá nhân, Vấn đề chất lượng, Khác...)
        /// 4. Điền thông tin ngân hàng (Tên ngân hàng, Số tài khoản, Tên chủ tài khoản)
        /// 5. Gửi đơn
        /// </summary>
        [HttpPost("submit")]
        [Authorize(Roles = "Learner")]
        public async Task<IActionResult> SubmitRefundRequest([FromBody] CreateRefundRequestDto dto)
        {
            try
            {
                var studentId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _refundRequestService.CreateRefundRequestAsync(dto, studentId);
                return Ok(new
                {
                    success = true,
                    message = "Gửi đơn hoàn tiền thành công. Chúng tôi sẽ xử lý trong vòng 3-5 ngày làm việc.",
                    data = result
                });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return Conflict(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo RefundRequest");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        // ================== DEPRECATED ==================

        /// <summary>
        /// [DEPRECATED] Sử dụng POST /admin/process thay thế
        /// </summary>
        [HttpPost("admin/send-email")]
        [Authorize(Roles = "Admin")]
        [Obsolete("Sử dụng POST /admin/process để xử lý và gửi email tự động")]
        public async Task<IActionResult> SendEmailToStudent([FromBody] RefundEmailDto dto)
        {
            return BadRequest(new
            {
                success = false,
                message = "API này đã ngừng sử dụng. Vui lòng sử dụng POST /api/refund/admin/process để xử lý đơn và gửi email tự động."
            });
        }
    }
}
