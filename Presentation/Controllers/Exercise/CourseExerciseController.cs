using BLL.IServices.Exercise;
using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Exercise
{
    [Route("api")]
    [ApiController]
    public class CourseExerciseController : ControllerBase
    {
        private readonly IExerciseService _exerciseService;
        public CourseExerciseController(IExerciseService exerciseService)
        {
            _exerciseService = exerciseService;
        }
        /// <summary>
        /// Creates a new exercise for a specified lesson.
        /// </summary>
        /// <param name="lessonId">The unique identifier of the lesson to which the exercise belongs.</param>
        /// <param name="request">The exercise creation request containing details and media files.</param>
        /// <returns>
        /// Returns a <see cref="BaseResponse{T}"/> containing the created exercise details if successful,
        /// or an error message otherwise.
        /// </returns>
        /// <response code="201">Exercise created successfully.</response>
        /// <response code="400">Invalid data or request format.</response>
        /// <response code="403">The user does not have permission to create exercises for this lesson.</response>
        /// <response code="404">Lesson or associated resource not found.</response>
        /// <response code="500">An internal server error occurred while processing the request.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPost("lessons/{lessonId:guid}/exercises")]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateExercise(Guid lessonId, [FromForm] ExerciseRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

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

            var result = await _exerciseService.CreateExerciseAsync(userId, lessonId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Updates an existing exercise within a specified lesson.
        /// </summary>
        /// <param name="lessonId">The unique identifier of the lesson that contains the exercise.</param>
        /// <param name="exerciseId">The unique identifier of the exercise to update.</param>
        /// <param name="request">The exercise update request containing new details and media files.</param>
        /// <returns>
        /// Returns a <see cref="BaseResponse{T}"/> containing the updated exercise details if successful,
        /// or an error message otherwise.
        /// </returns>
        /// <response code="200">Exercise updated successfully.</response>
        /// <response code="400">Invalid request data or media format.</response>
        /// <response code="403">The user does not have permission to modify this exercise.</response>
        /// <response code="404">Lesson or exercise not found.</response>
        /// <response code="500">An internal server error occurred while updating the exercise.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPut("lessons/{lessonId:guid}/exercises/{exerciseId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateExercise(Guid lessonId, Guid exerciseId, [FromForm] ExerciseUpdateRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _exerciseService.UpdateExerciseAsync(userId, lessonId, exerciseId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Retrieves a paginated list of exercises belonging to a specific lesson.
        /// </summary>
        /// <param name="lessonId">The unique identifier of the lesson.</param>
        /// <param name="request">The pagination request specifying page number and size.</param>
        /// <returns>
        /// Returns a <see cref="PagedResponse{T}"/> containing the list of exercises for the given lesson.
        /// </returns>
        /// <response code="200">Exercises retrieved successfully.</response>
        /// <response code="404">Lesson not found.</response>
        /// <response code="500">An internal server error occurred while retrieving exercises.</response>
        [AllowAnonymous]
        [HttpGet("lessons/{lessonId:guid}/exercises")]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<ExerciseResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<ExerciseResponse>>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<ExerciseResponse>>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetExercisesByLessonId(Guid lessonId, [FromQuery] PagingRequest request)
        {
            var result = await _exerciseService.GetExercisesByLessonIdAsync(lessonId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Retrieves detailed information of a specific exercise.
        /// </summary>
        /// <param name="exerciseId">The unique identifier of the exercise.</param>
        /// <returns>
        /// Returns a <see cref="BaseResponse{T}"/> containing exercise details if found,
        /// or an error message otherwise.
        /// </returns>
        /// <response code="200">Exercise retrieved successfully.</response>
        /// <response code="404">Exercise not found.</response>
        /// <response code="500">An internal server error occurred while retrieving exercise.</response>
        [AllowAnonymous]
        [HttpGet("exercises/{exerciseId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetExerciseById(Guid exerciseId)
        {
            var result = await _exerciseService.GetExerciseByIdAsync(exerciseId);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Deletes a specific exercise created by the authenticated teacher.
        /// </summary>
        /// <param name="exerciseId">The unique identifier of the exercise to delete.</param>
        /// <returns>
        /// Returns a <see cref="BaseResponse{T}"/> indicating whether the deletion was successful.
        /// </returns>
        /// <response code="200">Exercise deleted successfully.</response>
        /// <response code="403">The user does not have permission to delete this exercise.</response>
        /// <response code="404">Exercise not found.</response>
        /// <response code="500">An internal server error occurred while deleting the exercise.</response>
        [Authorize(Roles = "Teacher")]
        [HttpDelete("exercises/{exerciseId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteExerciseById(Guid exerciseId)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            var result = await _exerciseService.DeleteExerciseByIdAsync(userId, exerciseId);
            return StatusCode(result.Code, result);
        }
    }
}
