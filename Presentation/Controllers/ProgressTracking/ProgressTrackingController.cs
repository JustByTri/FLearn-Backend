using BLL.IServices.ProgressTracking;
using BLL.Services.Assessment;
using Common.DTO.ApiResponse;
using Common.DTO.Language;
using Common.DTO.ProgressTracking.Request;
using Common.DTO.ProgressTracking.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.ProgressTracking
{
    [Route("api/progress-tracking")]
    [ApiController]
    public class ProgressTrackingController : ControllerBase
    {
        private readonly IProgressTrackingService _progressTrackingService;
        public ProgressTrackingController(IProgressTrackingService progressTrackingService)
        {
            _progressTrackingService = progressTrackingService;
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("start-lesson")]
        public async Task<ActionResult<BaseResponse<ProgressTrackingResponse>>> StartLesson([FromBody] StartLessonRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _progressTrackingService.StartLessonAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("track-activity")]
        public async Task<ActionResult<BaseResponse<ProgressTrackingResponse>>> TrackActivity([FromBody] TrackActivityRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _progressTrackingService.TrackActivityAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("submit-exercise")]
        public async Task<ActionResult<BaseResponse<ExerciseSubmissionResponse>>> SubmitExercise([FromForm] SubmitExerciseRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _progressTrackingService.SubmitExerciseAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("current-progress/{courseId}")]
        public async Task<ActionResult<BaseResponse<ProgressTrackingResponse>>> GetCurrentProgress(Guid courseId)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _progressTrackingService.GetCurrentProgressAsync(userId, courseId);
            return StatusCode(result.Code, result);
        }
        [HttpGet("azure/transcribe/speech-to-text")]
        public async Task<IActionResult> TranscribeAudioToText(string audioUrl, [AllowedLang] string langCode)
        {
            var result = await AssessmentService.TranscribeSpeechByAzureAsync(audioUrl, langCode);
            return Ok(result);
        }
        [HttpGet("gemini/transcribe/speech-to-text")]
        public async Task<IActionResult> TranscribeSpeechToText(string audioUrl)
        {
            var result = await AssessmentService.TranscribeSpeechByGeminiAsync(audioUrl);
            return Ok(result);
        }
    }
}
