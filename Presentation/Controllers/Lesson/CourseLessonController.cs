using BLL.IServices.Lesson;
using Common.DTO.ApiResponse;
using Common.DTO.Lesson.Request;
using Common.DTO.Lesson.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Lesson
{
    [Route("api")]
    [ApiController]
    public class CourseLessonController : ControllerBase
    {
        private readonly ILessonService _lessonService;

        public CourseLessonController(ILessonService lessonService)
        {
            _lessonService = lessonService;
        }

        /// <summary>
        /// Get a paged list of lessons within a specific course unit.
        /// </summary>
        /// <param name="unitId">The unique identifier of the course unit.</param>
        /// <param name="request">Paging parameters (page, pageSize).</param>
        /// <remarks>Returns a paginated list of lessons that belong to the given unit.</remarks>
        /// <response code="200">Lessons retrieved successfully.</response>
        /// <response code="404">Unit not found or no lessons available.</response>
        [HttpGet("units/{unitId:guid}/lessons")]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<LessonResponse>>), 200)]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<LessonResponse>>), 404)]
        public async Task<IActionResult> GetLessonsByUnitId(Guid unitId, [FromQuery] PagingRequest request)
        {
            var result = await _lessonService.GetLessonsByUnitIdAsync(unitId, request);
            if (result.Meta.TotalItems == 0)
                return NotFound(result);

            return Ok(result);
        }

        /// <summary>
        /// Retrieve details of a specific lesson by its ID.
        /// </summary>
        /// <param name="lessonId">The unique identifier of the lesson.</param>
        /// <response code="200">Lesson retrieved successfully.</response>
        /// <response code="404">Lesson not found.</response>
        [HttpGet("lessons/{lessonId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 200)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 404)]
        public async Task<IActionResult> GetLessonById(Guid lessonId)
        {
            var result = await _lessonService.GetLessonByIdAsync(lessonId);
            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Create a new lesson under a specific unit.
        /// </summary>
        /// <param name="unitId">The unique identifier of the unit to which the lesson belongs.</param>
        /// <param name="request">The lesson creation request payload.</param>
        /// <response code="201">Lesson created successfully.</response>
        /// <response code="400">Invalid input or creation failed.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPost("units/{unitId:guid}/lessons")]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 201)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 400)]
        public async Task<IActionResult> CreateLessonAsync(Guid unitId, [FromForm] LessonRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _lessonService.CreateLessonAsync(userId, unitId, request);
            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Update an existing lesson.
        /// </summary>
        /// <param name="unitId">The unique identifier of the course unit that contains the lesson.</param>
        /// <param name="lessonId">The unique identifier of the lesson to update.</param>
        /// <param name="request">The request containing updated lesson data.</param>
        /// <remarks>
        /// Only teachers who own the course can update it.
        /// Only lessons in courses with status Draft or Rejected can be updated.
        /// </remarks>
        /// <response code="200">Lesson updated successfully.</response>
        /// <response code="400">Invalid request data or validation failure.</response>
        /// <response code="404">Lesson or unit not found.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPut("units/{unitId:guid}/lessons/{lessonId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 200)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 400)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 404)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 500)]
        public async Task<IActionResult> UpdateLessonAsync(Guid unitId, Guid lessonId, [FromForm] LessonUpdateRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");

            if (!ModelState.IsValid)
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));

            var result = await _lessonService.UpdateLessonAsync(userId, unitId, lessonId, request);
            return StatusCode(result.Code, result);
        }
    }
}
