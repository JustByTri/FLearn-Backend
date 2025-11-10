using BLL.IServices.Admin;
using Common.DTO.PayOut;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.Security.Claims;

namespace Presentation.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/payout")]
    [Authorize(Roles = "Admin,Manager")]
    public class PayoutAdminController : ControllerBase
    {
        private readonly IPayoutAdminService _payoutService;
        private readonly ILogger<PayoutAdminController> _logger;

        public PayoutAdminController(IPayoutAdminService payoutService, ILogger<PayoutAdminController> logger)
     {
     _payoutService = payoutService;
       _logger = logger;
        }

        /// <summary>
        /// L?y danh sách yêu c?u rút ti?n ?ang ch? x? lý
      /// </summary>
    [HttpGet("pending")]
        [ProducesResponseType(typeof(IEnumerable<PayoutRequestDetailDto>), 200)]
        public async Task<IActionResult> GetPendingRequests()
        {
            var adminUserId = this.GetUserId();
       var result = await _payoutService.GetPendingPayoutRequestsAsync(adminUserId);
            return this.ToActionResult(result);
        }

        /// <summary>
  /// L?y chi ti?t yêu c?u rút ti?n
        /// </summary>
        [HttpGet("{payoutRequestId}")]
        [ProducesResponseType(typeof(PayoutRequestDetailDto), 200)]
        public async Task<IActionResult> GetRequestDetail([FromRoute] Guid payoutRequestId)
        {
    var adminUserId = this.GetUserId();
       var result = await _payoutService.GetPayoutRequestDetailAsync(adminUserId, payoutRequestId);
   return this.ToActionResult(result);
      }

 /// <summary>
        /// X? lý yêu c?u rút ti?n (approve ho?c reject)
        /// </summary>
        /// <param name="payoutRequestId">ID yêu c?u rút ti?n</param>
        /// <param name="dto">Thông tin x? lý (approve/reject)</param>
        [HttpPost("{payoutRequestId}/process")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> ProcessRequest(
  [FromRoute] Guid payoutRequestId,
            [FromBody] ProcessPayoutRequestDto dto)
        {
       var adminUserId = this.GetUserId();
      var result = await _payoutService.ProcessPayoutRequestAsync(adminUserId, payoutRequestId, dto);
    return this.ToActionResult(result);
        }

        /// <summary>
      /// L?y t?t c? yêu c?u rút ti?n (có th? l?c theo tr?ng thái)
        /// </summary>
      /// <param name="status">Tr?ng thái: Pending, Completed, Rejected, Failed (optional)</param>
        [HttpGet("all")]
        [ProducesResponseType(typeof(IEnumerable<PayoutRequestDetailDto>), 200)]
    public async Task<IActionResult> GetAllRequests([FromQuery] string? status = null)
        {
          var adminUserId = this.GetUserId();
   var result = await _payoutService.GetAllPayoutRequestsAsync(adminUserId, status);
        return this.ToActionResult(result);
        }
    }
}
