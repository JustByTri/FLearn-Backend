using BLL.IServices.Subscription;
using Common.DTO.Subscription;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Subscription
{
    [Route("api/subscriptions")]
    [ApiController]
    [Authorize]
    public class SubscriptionController : ControllerBase
    {
        private readonly ISubscriptionService _subscriptionService;

        public SubscriptionController(ISubscriptionService subscriptionService)
        {
            _subscriptionService = subscriptionService;
        }

        [HttpPost("purchase")]
        public async Task<IActionResult> Purchase([FromBody] CreateSubscriptionPurchaseDto dto)
        {
            var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
            var result = await _subscriptionService.CreateSubscriptionPurchaseAsync(userId, dto.Plan);
            return Ok(new { success = true, data = result });
        }

        [HttpPost("callback")]
        [AllowAnonymous]
        public async Task<IActionResult> Callback([FromBody] SubscriptionCallbackDto callback)
        {
            var ok = await _subscriptionService.HandleCallbackAsync(callback);
            return Ok(new { success = ok });
        }
    }
}
