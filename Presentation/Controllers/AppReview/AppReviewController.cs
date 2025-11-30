using BLL.IServices.AI;
using BLL.IServices.AppReview;
using Common.DTO.AppReview.Request;
using Common.DTO.Paging.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.AppReview
{
    [Route("api/app-reviews")]
    [ApiController]
    public class AppReviewController : ControllerBase
    {
        private readonly IAppReviewService _appReviewService;
        private readonly IAIContentModerationService _moderationService;

        public AppReviewController(IAppReviewService appReviewService, IAIContentModerationService moderationService)
        {
            _appReviewService = appReviewService;
            _moderationService = moderationService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllReviews([FromQuery] PaginationParams @params)
        {
            var response = await _appReviewService.GetAllAppReviewsAsync(@params);
            return StatusCode(response.Code, response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMyReview()
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;

            var response = await _appReviewService.GetMyReviewAsync(userId);
            return StatusCode(response.Code, response);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] AppReviewRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isSafe = await _moderationService.IsContentSafeAsync(request.Content);
            if (!isSafe)
            {
                return BadRequest(new { message = "Your review contains inappropriate content violating our community standards and has been rejected." });
            }

            var response = await _appReviewService.CreateAppReviewAsync(userId, request);
            return StatusCode(response.Code, response);
        }

        [HttpPut("{reviewId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(Guid reviewId, [FromBody] AppReviewRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var isSafe = await _moderationService.IsContentSafeAsync(request.Content);
            if (!isSafe)
            {
                return BadRequest(new { message = "Your review contains inappropriate content violating our community standards and has been rejected." });
            }

            var response = await _appReviewService.UpdateAppReviewAsync(userId, reviewId, request);
            return StatusCode(response.Code, response);
        }

        [HttpDelete("{reviewId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid reviewId)
        {
            if (!this.TryGetUserId(out var userId, out var error)) return error!;

            var response = await _appReviewService.DeleteAppReviewAsync(userId, reviewId);
            return StatusCode(response.Code, response);
        }
    }
}
