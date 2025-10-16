using BLL.IServices.LessonProgress;
using Common.DTO.ApiResponse;
using Common.DTO.LessonProgress.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.LessonProgress
{
    [Route("api/lessons")]
    [ApiController]
    public class LessonProgressController : ControllerBase
    {
        private readonly ILessonProgressService _progressService;
        public LessonProgressController(ILessonProgressService progressService)
        {
            _progressService = progressService;
        }
        [Authorize(Policy = "OnlyLearner")]
        [HttpPost("{lessonId}/start")]
        public async Task<IActionResult> StartLesson([FromRoute] Guid lessonId, [FromBody] StartLessonRequest req)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _progressService.StartLessonAsync(userId, req.EnrollmentId, lessonId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Policy = "OnlyLearner")]
        [HttpPost("{lessonId}/progress")]
        public async Task<IActionResult> UpdateProgress([FromRoute] Guid lessonId, [FromBody] UpdateLessonProgressRequest req)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _progressService.UpdateLessonProgressAsync(userId, req.EnrollmentId, lessonId, req.ProgressPercent);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("{lessonId}/complete")]
        public async Task<IActionResult> CompleteLesson([FromRoute] Guid lessonId, [FromBody] CompleteLessonRequest req)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _progressService.CompleteLessonAsync(userId, req.EnrollmentId, lessonId, req.ForceComplete);
            return StatusCode(result.Code, result);
        }
        [Authorize(Policy = "OnlyLearner")]
        [HttpGet("{lessonId}/progress/{enrollmentId}")]
        public async Task<IActionResult> GetProgress([FromRoute] Guid lessonId, [FromRoute] Guid enrollmentId)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _progressService.GetLessonProgressAsync(userId, enrollmentId, lessonId);
            return StatusCode(result.Code, result);
        }
    }
}
