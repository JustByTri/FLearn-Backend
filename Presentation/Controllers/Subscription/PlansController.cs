using BLL.IServices.Subscription;
using Common.DTO.Paging.Request;
using Common.DTO.Subscription.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Subscription
{
    [Route("api/subscription-plans")]
    [ApiController]
    public class PlansController : ControllerBase
    {
        private readonly ISubscriptionPlanService _subscriptionPlanService;

        public PlansController(ISubscriptionPlanService subscriptionPlanService)
        {
            _subscriptionPlanService = subscriptionPlanService;
        }
        [HttpGet]
        public async Task<IActionResult> GetPlans([FromQuery] PagingRequest request)
        {
            var result = await _subscriptionPlanService.GetAllPlansAsync(request);
            return StatusCode(result.Code, result);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> GetPlanById(int id)
        {
            var result = await _subscriptionPlanService.GetPlanByIdAsync(id);
            return StatusCode(result.Code, result);
        }
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreatePlan([FromBody] SubscriptionPlanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _subscriptionPlanService.CreatePlanAsync(request);
            return StatusCode(result.Code, result);
        }
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdatePlan(int id, [FromBody] SubscriptionPlanRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _subscriptionPlanService.UpdatePlanAsync(id, request);
            return StatusCode(result.Code, result);
        }
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeletePlan(int id)
        {
            var result = await _subscriptionPlanService.DeletePlanAsync(id);
            return StatusCode(result.Code, result);
        }
    }
}
