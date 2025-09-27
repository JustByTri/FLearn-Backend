using BLL.IServices.Lesson;
using Common.DTO.ApiResponse;
using Common.DTO.Lesson.Request;
using Common.DTO.Lesson.Response;
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
    }
}
