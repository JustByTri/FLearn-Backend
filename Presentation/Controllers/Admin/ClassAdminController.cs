using BLL.IServices.Admin;
using Common.DTO.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Admin
{
    [Route("api/admin/classes")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class ClassAdminController : ControllerBase
    {
        private readonly IClassAdminService _classAdminService;

        public ClassAdminController(IClassAdminService classAdminService)
        {
            _classAdminService = classAdminService;
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
    }
}
