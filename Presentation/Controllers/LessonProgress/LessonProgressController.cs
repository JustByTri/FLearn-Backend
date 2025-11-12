using BLL.IServices.ProgressTracking;
using Common.DTO.ApiResponse;
using Common.DTO.LessonProgress.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.LessonProgress
{
    [Route("api/lesson-progress")]
    [ApiController]
    public class LessonProgressController : ControllerBase
    {
        private readonly ILessonProgressService _lessonProgressService;

        public LessonProgressController(ILessonProgressService lessonProgressService)
        {
            _lessonProgressService = lessonProgressService;
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("lessons/{lessonId}/progress")]
        public async Task<ActionResult<BaseResponse<LessonProgressDetailResponse>>> GetLessonProgress(Guid lessonId)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _lessonProgressService.GetLessonProgressAsync(userId, lessonId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("units/{unitId}/progress")]
        public async Task<ActionResult<BaseResponse<List<LessonProgressSummaryResponse>>>> GetUnitLessonsProgress(Guid unitId)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _lessonProgressService.GetUnitLessonsProgressAsync(userId, unitId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("lessons/{lessonId}/activity-status")]
        public async Task<ActionResult<BaseResponse<LessonActivityStatusResponse>>> GetLessonActivityStatus(Guid lessonId)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _lessonProgressService.GetLessonActivityStatusAsync(userId, lessonId);
            return StatusCode(result.Code, result);
        }
    }
}
