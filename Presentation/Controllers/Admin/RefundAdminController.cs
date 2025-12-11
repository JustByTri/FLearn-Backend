using BLL.IServices.Refund;
using Common.DTO.Refund;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Admin
{
    /// <summary>
    /// Controller qu?n lý ??n hoàn ti?n (Admin)
    /// 
    /// LU?NG HOÀN TI?N:
    /// ???????????????????????????????????????????????????????????????????????????????
    /// ?  1. T?o RefundRequest:                                                      ?
    /// ?     - L?p b? h?y ? Auto t?o (ClassCancelled)                               ?
    /// ?     - Dispute ???c ch?p nh?n ? Auto t?o (DisputeResolved)                  ?
    /// ?     - H?c viên t? yêu c?u ? Manual t?o (StudentPersonalReason)             ?
    /// ?                                                                             ?
    /// ?  2. Tr?ng thái:                                                             ?
    /// ?     Draft ? Pending ? UnderReview ? Approved ? Completed                   ?
    /// ?                    ? Rejected                                               ?
    /// ?                                                                             ?
    /// ?  3. Admin x? lý:                                                            ?
    /// ?     - Approve ? Chuy?n kho?n th? công ? Complete                           ?
    /// ?     - Reject ? G?i thông báo cho h?c viên                                  ?
    /// ???????????????????????????????????????????????????????????????????????????????
    /// 
    /// ENDPOINTS:
    /// - GET  /api/admin/refunds                    ? Danh sách ??n hoàn ti?n
    /// - GET  /api/admin/refunds/{refundId}         ? Chi ti?t ??n hoàn ti?n
    /// - POST /api/admin/refunds/{refundId}/approve ? Duy?t ??n
    /// - POST /api/admin/refunds/{refundId}/reject  ? T? ch?i ??n
    /// - POST /api/admin/refunds/{refundId}/complete? ?ánh d?u ?ã hoàn ti?n
    /// - GET  /api/admin/refunds/statistics         ? Th?ng kê
    /// 
    /// TODO: Implement các methods trong IRefundRequestService
    /// </summary>
    [Route("api/admin/refunds")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class RefundAdminController : ControllerBase
    {
        private readonly IRefundRequestService _refundService;
        private readonly ILogger<RefundAdminController> _logger;

        public RefundAdminController(
            IRefundRequestService refundService,
            ILogger<RefundAdminController> logger)
        {
            _refundService = refundService;
            _logger = logger;
        }

        /// <summary>
        /// [Admin] L?y danh sách ??n hoàn ti?n
        /// - Có th? l?c theo status, type, ngày
        /// 
        /// TODO: Implement GetAllRefundRequestsAsync trong IRefundRequestService
        /// </summary>
        /// <param name="status">Tr?ng thái: Draft, Pending, UnderReview, Approved, Rejected, Completed, Cancelled</param>
        /// <param name="type">Lo?i: ClassCancelled_InsufficientStudents, ClassCancelled_TeacherUnavailable, StudentPersonalReason, ClassQualityIssue, TechnicalIssue, Other, DisputeResolved</param>
        /// <param name="from">T? ngày</param>
        /// <param name="to">??n ngày</param>
        /// <param name="page">Trang</param>
        /// <param name="pageSize">S? l??ng m?i trang</param>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetAllRefundRequests(
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // TODO: Implement
            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetAllRefundRequestsAsync",
                data = new List<object>(),
                pagination = new
                {
                    currentPage = page,
                    pageSize = pageSize,
                    totalItems = 0,
                    totalPages = 0
                },
                summary = new
                {
                    totalPending = 0,
                    totalApproved = 0,
                    totalCompleted = 0,
                    totalAmount = 0
                }
            });
        }

        /// <summary>
        /// [Admin] Xem chi ti?t m?t ??n hoàn ti?n
        /// - Thông tin h?c viên
        /// - Thông tin l?p h?c/khóa h?c
        /// - Thông tin ngân hàng
        /// - L?ch s? x? lý
        /// 
        /// TODO: Implement GetRefundRequestDetailAsync trong IRefundRequestService
        /// </summary>
        /// <param name="refundId">ID ??n hoàn ti?n</param>
        [HttpGet("{refundId:guid}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> GetRefundRequestDetail(Guid refundId)
        {
            // TODO: Implement
            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetRefundRequestDetailAsync",
                data = new { refundId }
            });
        }

        /// <summary>
        /// [Admin] Duy?t ??n hoàn ti?n
        /// - Sau khi duy?t, Admin c?n chuy?n kho?n th? công cho h?c viên
        /// - Sau ?ó g?i API /complete ?? ?ánh d?u ?ã hoàn ti?n
        /// 
        /// TODO: Implement ApproveRefundRequestAsync trong IRefundRequestService
        /// </summary>
        /// <param name="refundId">ID ??n hoàn ti?n</param>
        /// <param name="dto">Ghi chú (optional)</param>
        [HttpPost("{refundId:guid}/approve")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> ApproveRefundRequest(Guid refundId, [FromBody] AdminRefundActionDto? dto)
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var adminId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement ApproveRefundRequestAsync. ?ã duy?t ??n hoàn ti?n."
            });
        }

        /// <summary>
        /// [Admin] T? ch?i ??n hoàn ti?n
        /// 
        /// TODO: Implement RejectRefundRequestAsync trong IRefundRequestService
        /// </summary>
        /// <param name="refundId">ID ??n hoàn ti?n</param>
        /// <param name="dto">Lý do t? ch?i (b?t bu?c)</param>
        [HttpPost("{refundId:guid}/reject")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> RejectRefundRequest(Guid refundId, [FromBody] AdminRefundActionDto dto)
        {
            // TODO: Implement
            if (string.IsNullOrWhiteSpace(dto?.Note))
                return BadRequest(new { success = false, message = "Vui lòng nh?p lý do t? ch?i" });

            if (!this.TryGetUserId(out var adminId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement RejectRefundRequestAsync. ?ã t? ch?i ??n hoàn ti?n."
            });
        }

        /// <summary>
        /// [Admin] ?ánh d?u ?ã hoàn ti?n (sau khi chuy?n kho?n th? công)
        /// - C?p nh?t tr?ng thái Completed
        /// - G?i thông báo cho h?c viên
        /// 
        /// TODO: Implement CompleteRefundRequestAsync trong IRefundRequestService
        /// </summary>
        /// <param name="refundId">ID ??n hoàn ti?n</param>
        /// <param name="dto">Mã giao d?ch ngân hàng (optional)</param>
        [HttpPost("{refundId:guid}/complete")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> CompleteRefundRequest(Guid refundId, [FromBody] CompleteRefundDto? dto)
        {
            // TODO: Implement
            if (!this.TryGetUserId(out var adminId, out var error))
                return error!;

            return Ok(new
            {
                success = true,
                message = "TODO: Implement CompleteRefundRequestAsync. ?ã ?ánh d?u hoàn ti?n thành công."
            });
        }

        /// <summary>
        /// [Admin] Xem th?ng kê ??n hoàn ti?n
        /// 
        /// TODO: Implement GetRefundStatisticsAsync trong IRefundRequestService
        /// </summary>
        [HttpGet("statistics")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetRefundStatistics(
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null)
        {
            // TODO: Implement
            return Ok(new
            {
                success = true,
                message = "TODO: Implement GetRefundStatisticsAsync",
                data = new
                {
                    totalRequests = 0,
                    totalPending = 0,
                    totalApproved = 0,
                    totalRejected = 0,
                    totalCompleted = 0,
                    totalAmount = 0
                }
            });
        }
    }

    /// <summary>
    /// DTO cho hành ??ng Admin v?i ??n hoàn ti?n
    /// </summary>
    public class AdminRefundActionDto
    {
        /// <summary>
        /// Ghi chú c?a Admin
        /// </summary>
        public string? Note { get; set; }
    }

    /// <summary>
    /// DTO ?ánh d?u hoàn ti?n
    /// </summary>
    public class CompleteRefundDto
    {
        /// <summary>
        /// Mã giao d?ch ngân hàng (?? ??i chi?u)
        /// </summary>
        public string? BankTransactionId { get; set; }
    }
}
