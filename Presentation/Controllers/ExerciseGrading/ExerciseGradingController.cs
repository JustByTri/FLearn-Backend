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
        /// <summary>
        /// Get the Course and Exercise list to display the Dropdown Filter.
        /// The API automatically recognizes the Role to return the appropriate data (Teacher only sees the Course he teaches, Manager sees everything).
        /// </summary>
        [Authorize]
        [HttpGet("filters")]
        public async Task<IActionResult> GetFilters()
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _exerciseGradingService.GetGradingFilterOptionsAsync(userId);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// [TEACHER ONLY] Get the list of assignments assigned to the currently logged in teacher.
        /// </summary>
        [HttpGet("teacher/assignments")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<BaseResponse<List<ExerciseGradingAssignmentResponse>>>> GetTeacherAssignments(
            [FromQuery] GradingAssignmentFilterRequest filter)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _exerciseGradingService.GetTeacherAssignmentsAsync(userId, filter);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Teacher grades an exercise submission.
        /// </summary>
        /// <param name="exerciseSubmissionId">ID of the submission being graded.</param>
        /// <param name="request">Score and feedback data.</param>
        /// <returns>Grading result response.</returns>
        [HttpPost("teacher/submissions/{exerciseSubmissionId}/grade")]
        [Authorize(Roles = "Teacher")]
        public async Task<ActionResult<BaseResponse<bool>>> ProcessTeacherGrading(Guid exerciseSubmissionId,
            [FromBody] TeacherGradingRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _exerciseGradingService.ProcessTeacherGradingAsync(
                exerciseSubmissionId, userId, request.Score, request.Feedback);

            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// [MANAGER ONLY] Get the list of exercises for the entire system.
        /// Used to filter out Expired exercises or find exercises by specific Teacher.
        /// </summary>
        [HttpGet("manager/assignments")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetManagerAssignments([FromQuery] GradingAssignmentFilterRequest filter)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("User ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _exerciseGradingService.GetManagerAssignmentsAsync(userId, filter);
            return StatusCode(result.Code, result);
        }
        [HttpGet("manager/eligible-teachers")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetEligibleTeachers([FromQuery] EligibleTeacherFilterRequest filter)
        {
            var result = await _exerciseGradingService.GetEligibleTeachersForReassignmentAsync(filter);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Assign exercise to teacher (Manager only)
        /// </summary>
        [HttpPost("manager/assignments")]
        [Authorize(Roles = "Manager")]
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
                    return Unauthorized("User ID not found in token.");

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                    return BadRequest("Invalid user ID format in token.");

                var result = await _exerciseGradingService.AssignExerciseToTeacherAsync(
                    request.ExerciseSubmissionId,
                    userId,
                    request.TeacherId);

                return StatusCode(result.Code, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, BaseResponse<object>.Error($"An error occurred while assigning exercise to teacher: {ex.Message}"));
            }
        }
        [HttpGet("status/{exerciseSubmissionId}")]
        public async Task<ActionResult<BaseResponse<ExerciseGradingStatusResponse>>> GetGradingStatus(Guid exerciseSubmissionId)
        {
            var result = await _exerciseGradingService.GetGradingStatusAsync(exerciseSubmissionId);
            return StatusCode(result.Code, result);
        }
        [HttpPost("retry-ai-grading/{submissionId:guid}")]
        [Authorize]
        public async Task<IActionResult> RetryAIGrading(Guid submissionId)
        {
            var result = await _exerciseGradingService.RetryAIGradingAsync(submissionId);

            return StatusCode(result.Code, result);
        }
    }
}
