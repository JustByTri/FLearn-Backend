using BLL.IServices.Lesson;
using Common.DTO.ApiResponse;
using Common.DTO.Lesson.Request;
using Common.DTO.Lesson.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
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
        /// Create a new lesson under a specific course and unit.
        /// </summary>
        /// <param name="courseId">The unique identifier of the course to which the lesson belongs.</param>
        /// <param name="unitId">The unique identifier of the unit to which the lesson belongs.</param>
        /// <param name="request">The lesson creation request payload.</param>
        /// <returns>A <see cref="BaseResponse{LessonResponse}"/> indicating success or failure.</returns>
        /// <response code="200">Lesson created successfully.</response>
        /// <response code="400">Invalid input or creation failed.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPost("courses/{courseId:guid}/units/{unitId:guid}/lessons")]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> CreateLessonAsync(
            Guid courseId,
            Guid unitId,
            [FromForm] LessonRequest request)
        {
            var teacherId = User.FindFirstValue("user_id")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(teacherId))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            Guid teacherGuid = Guid.Parse(teacherId);

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _lessonService.CreateLessonAsync(teacherGuid, courseId, unitId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Update an existing lesson information.
        /// </summary>
        /// <param name="teacherId">The unique identifier of the teacher performing the update.</param>
        /// <param name="courseId">The unique identifier of the course that contains the unit.</param>
        /// <param name="unitId">The unique identifier of the course unit that contains the lesson.</param>
        /// <param name="lessonId">The unique identifier of the lesson to update.</param>
        /// <param name="request">The request body containing updated lesson data, including optional new video/document files.</param>
        /// <remarks>
        /// This endpoint allows teachers to update an existing lesson.  
        /// Only lessons in courses with status **Draft** or **Rejected** can be updated.  
        /// If new files are provided, old video/document files will be replaced and deleted from storage.
        /// </remarks>
        /// <response code="200">Lesson updated successfully.</response>
        /// <response code="400">Invalid request data or validation failure.</response>
        /// <response code="404">Teacher, course, unit, or lesson not found.</response>
        /// <response code="500">Internal server error during update or file upload.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPut("courses/{courseId:guid}/units/{unitId:guid}/lessons/{lessonId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 200)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 400)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 404)]
        [ProducesResponseType(typeof(BaseResponse<LessonResponse>), 500)]
        public async Task<IActionResult> UpdateLessonAsync(
            Guid courseId,
            Guid unitId,
            Guid lessonId,
            [FromForm] LessonRequest request)
        {
            var teacherId = User.FindFirstValue("user_id")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(teacherId))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            Guid teacherGuid = Guid.Parse(teacherId);

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var result = await _lessonService.UpdateLessonAsync(teacherGuid, courseId, unitId, lessonId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Get a paged list of lessons within a specific course unit.
        /// </summary>
        /// <param name="unitId">The unique identifier of the course unit.</param>
        /// <param name="request">Paging parameters (page, pageSize).</param>
        /// <remarks>
        /// Returns a paginated list of lessons that belong to the given unit.
        /// </remarks>
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
        /// Retrieve the details of a specific lesson by its Id.
        /// </summary>
        /// <param name="lessonId">The unique identifier of the lesson.</param>
        /// <remarks>
        /// Returns detailed information about the requested lesson,
        /// including related course and unit details if available.
        /// </remarks>
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
        /// Get a paged list of all lessons that belong to a specific course.
        /// </summary>
        /// <param name="courseId">The unique identifier of the course.</param>
        /// <param name="request">Paging parameters (page, pageSize).</param>
        /// <remarks>
        /// Returns a paginated list of lessons across all units inside the given course.
        /// </remarks>
        /// <response code="200">Lessons retrieved successfully.</response>
        /// <response code="404">Course not found or no lessons available.</response>
        [HttpGet("courses/{courseId:guid}/lessons")]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<LessonResponse>>), 200)]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<LessonResponse>>), 404)]
        public async Task<IActionResult> GetLessonsByCourseId(Guid courseId, [FromQuery] PagingRequest request)
        {
            // Call the service with paging parameters
            var result = await _lessonService.GetLessonsByCourseIdAsync(courseId, request);

            // If no lessons found, return 404
            if (result.Meta.TotalItems == 0)
                return NotFound(result);

            return Ok(result);
        }
    }
}
