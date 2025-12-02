using BLL.IServices.Admin;
using Common.DTO.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Admin
{
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

        /// <summary>
        /// Get all disputes for review
        /// </summary>
        [HttpGet("disputes")]
        public async Task<IActionResult> GetDisputes()
        {
            var disputes = await _classAdminService.GetAllDisputesAsync();
            return Ok(new { success = true, data = disputes });
        }

        /// <summary>
        /// Admin resolves a dispute (refund/partial/refuse)
        /// </summary>
        [HttpPost("disputes/{disputeId:guid}/resolve")]
        public async Task<IActionResult> ResolveDispute(Guid disputeId, [FromBody] ResolveDisputeDto dto)
        {
            var result = await _classAdminService.ResolveDisputeAsync(disputeId, dto);
            return Ok(new { success = result, message = result ? "Dispute resolved." : "Failed to resolve dispute." });
        }

        /// <summary>
        /// Trigger payout for a class
        /// </summary>
        [HttpPost("{classId:guid}/payout")]
        public async Task<IActionResult> TriggerPayout(Guid classId)
        {
            var result = await _classAdminService.TriggerPayoutAsync(classId);
            return Ok(new { success = result, message = result ? "Payout triggered." : "Failed to trigger payout." });
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
    

