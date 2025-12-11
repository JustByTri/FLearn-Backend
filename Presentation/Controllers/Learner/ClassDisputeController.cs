using BLL.IServices.Dispute;
using Common.DTO.Dispute;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Learner
{
    /// <summary>
    /// Controller cho học viên tạo và quản lý đơn khiếu nại lớp học
    /// </summary>
    [Route("api/student/disputes")]
    [ApiController]
    [Authorize(Roles = "Learner")]
    public class ClassDisputeController : ControllerBase
    {
        private readonly IClassDisputeService _disputeService;
        private readonly ILogger<ClassDisputeController> _logger ;

        public ClassDisputeController(
            IClassDisputeService disputeService,
            ILogger<ClassDisputeController> logger)
        {
            _disputeService = disputeService;
            _logger = logger;
        }

        /// <summary>
        /// [Học viên] Tạo đơn khiếu nại sau khi lớp học kết thúc
        /// - Chỉ được khiếu nại trong vòng 3 ngày sau khi lớp kết thúc
        /// - Phải đã thanh toán và tham gia lớp học
        /// </summary>
        /// <param name="dto">Thông tin đơn khiếu nại</param>
        [HttpPost]
        [ProducesResponseType(typeof(object), 201)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 401)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> CreateDispute([FromBody] CreateDisputeDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var studentId, out var error))
                    return error!;

                var result = await _disputeService.CreateDisputeAsync(studentId, dto);

                return CreatedAtAction(
                    nameof(GetDisputeById),
                    new { disputeId = result.DisputeId },
                    new
                    {
                        success = true,
                        message = "Đơn khiếu nại đã được tạo thành công. Chúng tôi sẽ xem xét và phản hồi trong thời gian sớm nhất.",
                        data = result
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating dispute");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Học viên] Lấy danh sách đơn khiếu nại của mình
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetMyDisputes()
        {
            try
            {
                if (!this.TryGetUserId(out var studentId, out var error))
                    return error!;

                var disputes = await _disputeService.GetMyDisputesAsync(studentId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách đơn khiếu nại thành công",
                    data = disputes,
                    summary = new
                    {
                        total = disputes.Count,
                        pending = disputes.Count(d => d.Status == "Open" || d.Status == "Submmitted" || d.Status == "UnderReview"),
                        resolved = disputes.Count(d => d.Status.StartsWith("Resolved")),
                        closed = disputes.Count(d => d.Status == "Closed")
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting disputes");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Học viên] Lấy chi tiết một đơn khiếu nại
        /// </summary>
        /// <param name="disputeId">ID của đơn khiếu nại</param>
        [HttpGet("{disputeId:guid}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetDisputeById(Guid disputeId)
        {
            try
            {
                if (!this.TryGetUserId(out var studentId, out var error))
                    return error!;

                var dispute = await _disputeService.GetDisputeByIdAsync(studentId, disputeId);

                if (dispute == null)
                    return NotFound(new { success = false, message = "Không tìm thấy đơn khiếu nại" });

                return Ok(new
                {
                    success = true,
                    data = dispute
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dispute {DisputeId}", disputeId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Học viên] Hủy đơn khiếu nại (chỉ khi đang ở trạng thái Open/Submitted)
        /// </summary>
        /// <param name="disputeId">ID của đơn khiếu nại</param>
        [HttpDelete("{disputeId:guid}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> CancelDispute(Guid disputeId)
        {
            try
            {
                if (!this.TryGetUserId(out var studentId, out var error))
                    return error!;

                await _disputeService.CancelDisputeAsync(studentId, disputeId);

                return Ok(new
                {
                    success = true,
                    message = "Đơn khiếu nại đã được hủy thành công"
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
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling dispute {DisputeId}", disputeId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }
    }
}
