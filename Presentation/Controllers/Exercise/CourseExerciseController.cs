using BLL.IServices.Exercise;
using Common.DTO.ApiResponse;
using Common.DTO.Exercise.Request;
using Common.DTO.Exercise.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
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
        /// Create a new exercise under a specific lesson.
        /// </summary>
        /// <param name="courseId">The ID of the course the exercise belongs to.</param>
        /// <param name="unitId">The ID of the unit the exercise belongs to.</param>
        /// <param name="lessonId">The ID of the lesson the exercise belongs to.</param>
        /// <param name="request">The exercise request object containing exercise details.</param>
        /// <returns>A response containing the created exercise details or an error message.</returns>
        [Authorize(Roles = "Teacher")]
        [HttpPost("courses/{courseId:guid}/units/{unitId:guid}/lessons/{lessonId:guid}/exercises")]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), 200)]
        [ProducesResponseType(typeof(BaseResponse<ExerciseResponse>), 400)]
        public async Task<IActionResult> CreateExercise(
            Guid courseId,
            Guid unitId,
            Guid lessonId,
            [FromForm] ExerciseRequest request)
        {
            var idClaim = User.FindFirst("user_id")?.Value
                       ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? User.FindFirst("sub")?.Value
                       ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrWhiteSpace(idClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(idClaim, out Guid teacherGuid) || teacherGuid == Guid.Empty)
            {
                return Unauthorized("Invalid teacher ID in token.");
            }

            if (Request.Form.ContainsKey("Options"))
            {
                var optionValues = Request.Form["Options"];
                var parsedList = new List<ExerciseOptionRequest>();
                foreach (var val in optionValues)
                {
                    try
                    {
                        var option = JsonConvert.DeserializeObject<ExerciseOptionRequest>(val);
                        if (option != null)
                        {
                            parsedList.Add(option);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Parse error: {ex.Message}");
                    }
                }

                request.Options = parsedList;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }
            var result = await _exerciseService.CreateExerciseAsync(teacherGuid, courseId, unitId, lessonId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Update an existing exercise.
        /// </summary>
        /// <param name="courseId">The ID of the course.</param>
        /// <param name="unitId">The ID of the unit.</param>
        /// <param name="lessonId">The ID of the lesson.</param>
        /// <param name="exerciseId">The ID of the exercise to update.</param>
        /// <param name="request">The updated exercise request data.</param>
        /// <returns>Returns the updated exercise details.</returns>
        [Authorize(Roles = "Teacher")]
        [HttpPut("courses/{courseId:guid}/units/{unitId:guid}/lessons/{lessonId:guid}/exercises/{exerciseId:guid}")]
        public async Task<ActionResult<BaseResponse<ExerciseResponse>>> UpdateExercise(
            Guid courseId,
            Guid unitId,
            Guid lessonId,
            Guid exerciseId,
            [FromForm] ExerciseRequest request)
        {
            var idClaim = User.FindFirst("user_id")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrWhiteSpace(idClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(idClaim, out Guid teacherGuid) || teacherGuid == Guid.Empty)
            {
                return Unauthorized("Invalid teacher ID in token.");
            }

            if (Request.Form.ContainsKey("Options"))
            {
                var optionValues = Request.Form["Options"];
                var parsedList = new List<ExerciseOptionRequest>();
                foreach (var val in optionValues)
                {
                    try
                    {
                        var option = JsonConvert.DeserializeObject<ExerciseOptionRequest>(val);
                        if (option != null)
                        {
                            parsedList.Add(option);
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Parse error: {ex.Message}");
                    }
                }

                request.Options = parsedList;
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _exerciseService.UpdateExerciseAsync(teacherGuid, courseId, unitId, lessonId, exerciseId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Get a paginated list of exercises for a specific lesson.
        /// </summary>
        /// <param name="lessonId">The ID of the lesson.</param>
        /// <param name="paging">The paging parameters (pageNumber, pageSize).</param>
        /// <returns>Returns a paginated list of exercises.</returns>
        [HttpGet("lessons/{lessonId:guid}/exercises")]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResponse<IEnumerable<ExerciseResponse>>>> GetExercisesByLesson(
            Guid lessonId,
            [FromQuery] PagingRequest paging)
        {
            var result = await _exerciseService.GetExercisesByLessonIdAsync(lessonId, paging);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Get details of a specific exercise by its ID.
        /// </summary>
        /// <param name="exerciseId">The ID of the exercise.</param>
        /// <returns>Returns the exercise details.</returns>
        [HttpGet("exercises/{exerciseId:guid}")]
        [AllowAnonymous]
        public async Task<ActionResult<BaseResponse<ExerciseResponse>>> GetExerciseById(Guid exerciseId)
        {
            var result = await _exerciseService.GetExerciseByIdAsync(exerciseId);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Delete an exercise by its ID.
        /// </summary>
        /// <param name="courseId">The ID of the course.</param>
        /// <param name="unitId">The ID of the unit.</param>
        /// <param name="lessonId">The ID of the lesson.</param>
        /// <param name="exerciseId">The ID of the exercise to delete.</param>
        /// <returns>Returns the deleted exercise details.</returns>
        [Authorize(Roles = "Teacher")]
        [HttpDelete("courses/{courseId:guid}/units/{unitId:guid}/lessons/{lessonId:guid}/exercises/{exerciseId:guid}")]
        public async Task<ActionResult<BaseResponse<ExerciseResponse>>> DeleteExercise(
            Guid courseId,
            Guid unitId,
            Guid lessonId,
            Guid exerciseId)
        {
            var idClaim = User.FindFirst("user_id")?.Value
                ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? User.FindFirst("sub")?.Value
                ?? User.FindFirst("id")?.Value;

            if (string.IsNullOrWhiteSpace(idClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(idClaim, out Guid teacherGuid) || teacherGuid == Guid.Empty)
            {
                return Unauthorized("Invalid teacher ID in token.");
            }

            var result = await _exerciseService.DeleteExerciseAsync(teacherGuid, courseId, unitId, lessonId, exerciseId);
            return StatusCode(result.Code, result);
        }
    }
}
