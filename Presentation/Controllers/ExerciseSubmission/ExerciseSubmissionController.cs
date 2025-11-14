using BLL.IServices.ProgressTracking;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.ExerciseSubmission
{
    [Route("api")]
    [ApiController]
    public class ExerciseSubmissionController : ControllerBase
    {
        private readonly IProgressTrackingService _progressTrackingService;
        public ExerciseSubmissionController(IProgressTrackingService progressTrackingService)
        {
            _progressTrackingService = progressTrackingService;
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("courses/{courseId:guid}/lessons/{lessonId:guid}/exercise-submission/my-submissions")]
        public async Task<IActionResult> GetMyExerciseSubmission(Guid? courseId, Guid? lessonId, string? status, [FromQuery] int pageNumber, [FromQuery] int pageSize)
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

            var result = await _progressTrackingService.GetMySubmissionsAsync(userId, courseId, lessonId, status, pageNumber, pageSize);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("exercise-submission/submissions/{submissionId:guid}")]
        public async Task<IActionResult> GetSubmissionDetail(Guid submissionId)
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

            var result = await _progressTrackingService.GetSubmissionDetailAsync(userId, submissionId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("exercise-submission/exercises/{exerciseId:guid}/submissions")]
        public async Task<IActionResult> GetSubmissionHistory(Guid exerciseId, [FromQuery] int pageNumber, [FromQuery] int pageSize)
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

            var result = await _progressTrackingService.GetExerciseSubmissionsHistoryAsync(userId, exerciseId, pageNumber, pageSize);
            return StatusCode(result.Code, result);
        }
    }
}
