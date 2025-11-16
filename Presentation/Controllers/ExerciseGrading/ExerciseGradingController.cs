using BLL.IServices.ProgressTracking;
using Common.DTO.ApiResponse;
using Common.DTO.ExerciseGrading.Request;
using Common.DTO.ExerciseGrading.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.ExerciseGrading
{
    [Route("api/exercise-grading")]
    [ApiController]
    public class ExerciseGradingController : ControllerBase
    {
        private readonly IExerciseGradingService _exerciseGradingService;
        public ExerciseGradingController(IExerciseGradingService exerciseGradingService)
        {
            _exerciseGradingService = exerciseGradingService;
        }
        [HttpPost("teacher-grade/{exerciseSubmissionId}")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<BaseResponse<bool>>> ProcessTeacherGrading(Guid exerciseSubmissionId,
            [FromBody] TeacherGradingRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _exerciseGradingService.ProcessTeacherGradingAsync(
                exerciseSubmissionId, userId, request.Score, request.Feedback);

            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Assign exercise to teacher (Manager only)
        /// </summary>
        [HttpPost("assign-exercise")]
        [Authorize(Roles = "Manager,Admin")]
        [ProducesResponseType(typeof(BaseResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<object>), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AssignExerciseToTeacher([FromBody] AssignExerciseRequest request)
        {
            try
            {
                var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userIdClaim))
                    return Unauthorized("Teacher ID not found in token.");

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                    return BadRequest("Invalid user ID format in token.");

                if (request.ExerciseSubmissionId == Guid.Empty)
                    return BadRequest(BaseResponse<object>.Fail(null, "ExerciseSubmissionId is required", 400));

                if (request.TeacherId == Guid.Empty)
                    return BadRequest(BaseResponse<object>.Fail(null, "TeacherId is required", 400));

                var result = await _exerciseGradingService.AssignExerciseToTeacherAsync(
                    request.ExerciseSubmissionId,
                    userId,
                    request.TeacherId);

                return StatusCode(result.Code, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Error("An error occurred while assigning exercise to teacher"));
            }
        }
        [HttpGet("status/{exerciseSubmissionId}")]
        public async Task<ActionResult<BaseResponse<ExerciseGradingStatusResponse>>> GetGradingStatus(Guid exerciseSubmissionId)
        {
            var result = await _exerciseGradingService.GetGradingStatusAsync(exerciseSubmissionId);
            return StatusCode(result.Code, result);
        }
        [HttpGet("teacher-assignments")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<BaseResponse<List<ExerciseGradingAssignmentResponse>>>> GetTeacherAssignments(
            [FromQuery] GradingAssignmentFilterRequest filter)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _exerciseGradingService.GetTeacherAssignmentsAsync(userId, filter);
            return StatusCode(result.Code, result);
        }
    }
}
