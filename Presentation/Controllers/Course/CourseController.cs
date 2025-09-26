using BLL.IServices.Course;
using Common.DTO.ApiResponse;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Course
{
    [Route("api/courses")]
    [ApiController]
    public class CourseController : ControllerBase
    {
        private readonly ICourseService _courseService;
        public CourseController(ICourseService courseService)
        {
            _courseService = courseService;
        }
        /// <summary>
        /// Retrieves a paginated list of courses.
        /// </summary>
        /// <param name="request">Pagination information: Page, PageSize</param>
        /// <returns>A paginated list of courses</returns>
        /// <response code="200">Successfully retrieved the list of courses</response>
        /// <response code="404">No courses found</response>
        /// <response code="500">Server error during course retrieval</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<CourseResponse>>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetAllCourses([FromQuery] PagingRequest request)
        {
            try
            {
                var response = await _courseService.GetAllCoursesAsync(request);

                if (response.Data == null || !response.Data.Any())
                {
                    return NotFound(new
                    {
                        Message = "No courses found",
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = response.Meta.TotalItems
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching courses",
                    Details = ex.Message
                });
            }
        }
        /// <summary>
        /// Retrieves the details of a course by its ID.
        /// </summary>
        /// <param name="courseId">The ID of the course</param>
        /// <returns>Detailed information about the course</returns>
        /// <response code="200">Course found successfully</response>
        /// <response code="404">Course not found</response>
        /// <response code="500">Server error during course details retrieval</response>
        [HttpGet("{courseId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<CourseResponse>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCourseById(Guid courseId)
        {
            try
            {
                var response = await _courseService.GetCourseByIdAsync(courseId);

                if (response.Data == null)
                    return NotFound(new { Message = "Course not found" });

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching course details",
                    Details = ex.Message
                });
            }
        }
        /// <summary>
        /// Create a new course with topics, teacher, template, and other details.
        /// </summary>
        /// <param name="request">Course request model including title, description, image, etc.</param>
        /// <returns>
        /// Returns a <see cref="CourseResponse"/> wrapped inside <c>BaseResponse</c>.
        /// If success, HTTP 200 (OK) with course data.
        /// If validation fails, HTTP 400 (Bad Request).
        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<IActionResult> Create([FromForm] CourseRequest request)
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

            var response = await _courseService.CreateCourseAsync(teacherGuid, request);

            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Update a course (only Draft or Rejected courses can be updated).
        /// </summary>
        /// <param name="courseId">ID of the course to update</param>
        /// <param name="request">Updated course information</param>
        /// <returns>Updated course details</returns>
        /// <remarks>
        /// - Only the teacher who created the course can update it.  
        /// - Status will remain Draft after update until submitted for review.  
        /// - If a new image is uploaded, the old one will be deleted from Cloudinary.  
        /// </remarks>
        [HttpPut("{courseId:guid}")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(BaseResponse<CourseResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(BaseResponse<CourseResponse>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<CourseResponse>), StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> UpdateCourse(Guid courseId, [FromForm] UpdateCourseRequest request)
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

            var response = await _courseService.UpdateCourseAsync(teacherGuid, courseId, request);

            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Retrieves all courses created by a specific teacher.
        /// </summary>
        /// <param name="request">The paging request containing page number and page size.</param>
        /// <returns>
        /// A paged response containing a list of courses created by the given teacher. 
        /// If the teacher is not found or does not have the "Teacher" role, 
        /// an error will be returned.
        /// </returns>
        /// <response code="200">Returns the list of courses for the teacher.</response>
        /// <response code="401">If the user does not have the Teacher role.</response>
        /// <response code="404">If the teacher with the given ID does not exist.</response>
        [HttpGet("by-teacher")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<CourseResponse>>), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetAllCoursesByTeacherId([FromQuery] PagingRequest request)
        {
            var teacherId = User.FindFirstValue("user_id")
                    ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(teacherId))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            Guid teacherGuid = Guid.Parse(teacherId);

            var response = await _courseService.GetAllCoursesByTeacherIdAsync(teacherGuid, request);

            return StatusCode(response.Code, response);
        }
    }
}
