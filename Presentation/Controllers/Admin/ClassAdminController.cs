using BLL.IServices.Admin;
using Common.DTO.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Admin
{
    /// <summary>
    /// Controller quản lý lớp học (Admin/Manager)
    /// - Quản lý yêu cầu hủy lớp từ giáo viên
    /// - Xem và xử lý đơn khiếu nại từ học viên
    /// - Trigger payout thủ công (nếu cần)
    /// </summary>
    [Route("api/admin/classes")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ClassAdminController : ControllerBase
    {
        private readonly IClassAdminService _classAdminService;
        private readonly ILogger<ClassAdminController> _logger;
        
        public ClassAdminController(IClassAdminService classAdminService, ILogger<ClassAdminController> logger)
        {
            _classAdminService = classAdminService;
            _logger = logger;
        }

        #region Cancellation Requests (Yêu cầu hủy lớp từ Giáo viên)

        /// <summary>
        /// [Manager] Lấy danh sách yêu cầu hủy lớp đang chờ duyệt
        /// - Chỉ hiển thị các yêu cầu thuộc ngôn ngữ mà Manager quản lý
        /// </summary>
        [HttpGet("cancellation-requests")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetPendingCancellationRequests()
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
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cancellation requests");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Manager] Xem chi tiết một yêu cầu hủy lớp
        /// - Thông tin lớp học
        /// - Thông tin giáo viên
        /// - Số học viên đã đăng ký
        /// - Tổng tiền cần hoàn
        /// </summary>
        /// <param name="requestId">ID yêu cầu hủy</param>
        [HttpGet("cancellation-requests/{requestId:guid}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetCancellationRequestDetail(Guid requestId)
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
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cancellation request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Manager] Duyệt yêu cầu hủy lớp
        /// - Tự động tạo RefundRequest cho tất cả học viên đã đăng ký
        /// - Gửi thông báo cho giáo viên và học viên
        /// </summary>
        /// <param name="requestId">ID yêu cầu hủy</param>
        /// <param name="dto">Ghi chú (optional)</param>
        [HttpPost("cancellation-requests/{requestId:guid}/approve")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> ApproveCancellationRequest(Guid requestId, [FromBody] ManagerNoteDto? dto)
        {
            try
            {
                if (!this.TryGetUserId(out var managerId, out var error))
                    return error!;

                var result = await _classAdminService.ApproveCancellationRequestAsync(managerId, requestId, dto?.Note);

                return Ok(new
                {
                    success = true,
                    message = "Đã duyệt yêu cầu hủy lớp. Học viên sẽ được hoàn tiền."
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
                return Unauthorized(new { success = false, message = ex.Message });
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
        /// <param name="requestId">ID yêu cầu hủy</param>
        /// <param name="dto">Lý do từ chối (bắt buộc)</param>
        [HttpPost("cancellation-requests/{requestId:guid}/reject")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> RejectCancellationRequest(Guid requestId, [FromBody] ManagerNoteDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Note))
                    return BadRequest(new { success = false, message = "Vui lòng nhập lý do từ chối" });

                if (!this.TryGetUserId(out var managerId, out var error))
                    return error!;

                var result = await _classAdminService.RejectCancellationRequestAsync(managerId, requestId, dto.Note);

                return Ok(new
                {
                    success = true,
                    message = "Đã từ chối yêu cầu hủy lớp"
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
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rejecting cancellation request {RequestId}", requestId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        #endregion

        #region Disputes (Đơn khiếu nại từ Học viên)

        /// <summary>
        /// [Admin] Lấy danh sách tất cả đơn khiếu nại
        /// - Chỉ hiển thị các đơn đang chờ xử lý (Open, UnderReview, Submitted)
        /// </summary>
        [HttpGet("disputes")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetDisputes()
        {
            try
            {
                var disputes = await _classAdminService.GetAllDisputesAsync();

                return Ok(new
                {
                    success = true,
                    data = disputes,
                    count = disputes.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disputes");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Admin] Giải quyết đơn khiếu nại
        /// - Resolution: "refund" (hoàn 100%), "partial" (hoàn 50%), "refuse" (từ chối)
        /// - Sau khi resolve, hệ thống sẽ tự động:
        ///   + Tạo RefundRequest nếu chấp nhận
        ///   + Tính lại tiền payout cho giáo viên
        /// </summary>
        /// <param name="disputeId">ID đơn khiếu nại</param>
        /// <param name="dto">Kết quả xử lý</param>
        [HttpPost("disputes/{disputeId:guid}/resolve")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> ResolveDispute(Guid disputeId, [FromBody] ResolveDisputeDto dto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto?.Resolution))
                    return BadRequest(new { success = false, message = "Vui lòng chọn kết quả xử lý" });

                var validResolutions = new[] { "refund", "partial", "refuse" };
                if (!validResolutions.Contains(dto.Resolution.ToLower()))
                    return BadRequest(new { success = false, message = "Resolution không hợp lệ. Chọn: refund, partial, refuse" });

                var result = await _classAdminService.ResolveDisputeAsync(disputeId, dto);

                if (!result)
                    return BadRequest(new { success = false, message = "Không thể xử lý đơn khiếu nại" });

                var message = dto.Resolution.ToLower() switch
                {
                    "refund" => "Đã chấp nhận khiếu nại. Học viên sẽ được hoàn 100% tiền.",
                    "partial" => "Đã chấp nhận khiếu nại một phần. Học viên sẽ được hoàn 50% tiền.",
                    "refuse" => "Đã từ chối khiếu nại.",
                    _ => "Đã xử lý khiếu nại."
                };

                return Ok(new
                {
                    success = true,
                    message = message
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resolving dispute {DisputeId}", disputeId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        #endregion

        #region Payout (Thanh toán cho Giáo viên)

        /// <summary>
        /// [Admin] Trigger payout thủ công cho một lớp học
        /// - Chỉ dùng trong trường hợp đặc biệt
        /// - Bình thường hệ thống sẽ tự động payout qua ClassLifecycleService
        /// </summary>
        /// <param name="classId">ID lớp học</param>
        [HttpPost("{classId:guid}/payout")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        public async Task<IActionResult> TriggerPayout(Guid classId)
        {
            try
            {
                var result = await _classAdminService.TriggerPayoutAsync(classId);

                if (!result)
                    return BadRequest(new { success = false, message = "Không thể trigger payout. Lớp phải ở trạng thái Completed_PendingPayout." });

                return Ok(new
                {
                    success = true,
                    message = "Đã trigger payout thành công"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error triggering payout for class {ClassId}", classId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        #endregion
    }

    /// <summary>
    /// DTO ghi chú của Manager
    /// </summary>
    public class ManagerNoteDto
    {
        public string? Note { get; set; }
    }
}

