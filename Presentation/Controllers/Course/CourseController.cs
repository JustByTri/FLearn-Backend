using BLL.IServices.Course;
using Common.DTO.ApiResponse;
using Common.DTO.Application.Request;
using Common.DTO.Course;
using Common.DTO.Course.Request;
using Common.DTO.Course.Response;
using Common.DTO.Language;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Net;
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
        /// Retrieves a paginated list of courses, optionally filtered by course status.
        /// </summary>
        /// <param name="request">
        /// Pagination information: <c>Page</c>, <c>PageSize</c>.
        /// </param>
        /// <param name="status">
        /// Optional filter by course status. Accepted values:
        /// <list type="bullet">
        /// <item><description><c>Draft</c></description></item>
        /// <item><description><c>PendingApproval</c></description></item>
        /// <item><description><c>Published</c></description></item>
        /// <item><description><c>Rejected</c></description></item>
        /// <item><description><c>Archived</c></description></item>
        /// </list>
        /// </param>
        /// <param name="lang">
        /// Optional filter by lang code. Accepted values:
        /// <list type="bullet">
        /// <item><description><c>en</c></description></item>
        /// <item><description><c>ja</c></description></item>
        /// <item><description><c>zh</c></description></item>
        /// </list>
        /// </param>
        /// <returns>A paginated list of courses.</returns>
        /// <response code="200">Successfully retrieved the list of courses.</response>
        /// <response code="400">Invalid status value.</response>
        /// <response code="404">No courses found.</response>
        /// <response code="500">Server error during course retrieval.</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<CourseResponse>>), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetAllCourses([FromQuery] PagingRequest request, [FromQuery] string? status, [FromQuery][AllowedLang] string? lang)
        {
            try
            {
                var response = await _courseService.GetCoursesAsync(request, status, lang);

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

                return StatusCode(response.Code, response);
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
        [HttpGet("{courseId:guid}/details")]
        [ProducesResponseType(typeof(BaseResponse<CourseResponse>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetCourseDetailsById(Guid courseId)
        {
            try
            {
                var response = await _courseService.GetCourseDetailsByIdAsync(courseId);

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
        /// Creates a new course submitted by an authorized teacher.
        /// </summary>
        /// <param name="request">
        /// The course creation request containing title, description, image, template ID,
        /// topic IDs, price, discount, course type, goal, and level information.
        /// </param>
        /// <returns>
        /// Returns an <see cref="IActionResult"/> containing:
        /// <list type="bullet">
        /// <item><description><b>201 Created</b> - if the course is successfully created.</description></item>
        /// <item><description><b>400 Bad Request</b> - if the request data or user ID is invalid.</description></item>
        /// <item><description><b>401 Unauthorized</b> - if the teacher ID is missing or invalid in the JWT token.</description></item>
        /// </list>
        /// </returns>
        /// <remarks>
        /// This endpoint is restricted to users with the <c>Teacher</c> role.
        /// </remarks>
        [Authorize(Roles = "Teacher")]
        [HttpPost]
        public async Task<IActionResult> CreateCourse([FromForm] CourseRequest request)
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

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var response = await _courseService.CreateCourseAsync(userId, request);

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
        [ProducesResponseType(typeof(BaseResponse<CourseResponse>), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateCourse(Guid courseId, [FromForm] UpdateCourseRequest request)
        {
            try
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

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(e => e.Value?.Errors.Count > 0)
                        .ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value!.Errors.First().ErrorMessage
                        );

                    return BadRequest(BaseResponse<object>.Fail(errors, "Invalid request data.", 400));
                }

                var response = await _courseService.UpdateCourseAsync(userId, courseId, request);

                return StatusCode(response.Code, response);
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    BaseResponse<object>.Error("An unexpected error occurred while updating the course."));
            }
        }
        /// <summary>
        /// Retrieves all courses created by the currently authenticated teacher.
        /// </summary>
        /// <param name="request">The paging request containing page number and page size.</param>
        /// <param name="status">
        /// (Optional) Filter courses by status.  
        /// Accepted values: Draft, PendingApproval, Published, Rejected, Archived.
        /// </param>
        /// <returns>
        /// A paged response containing the teacher's courses.  
        /// Returns 404 if the teacher does not exist or has no courses.
        /// </returns>
        /// <response code="200">Successfully retrieved courses.</response>
        /// <response code="400">If the status value is invalid or user ID format is incorrect.</response>
        /// <response code="401">If the user is not authenticated or not a Teacher.</response>
        /// <response code="404">If no teacher or courses found.</response>
        /// <response code="500">If an unexpected error occurs.</response>
        [HttpGet("by-teacher")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<CourseResponse>>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(object), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(object), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllCoursesByTeacherId([FromQuery] PagingRequest request, [FromQuery] string? status)
        {
            try
            {
                var userIdClaim = User.FindFirstValue("user_id")
                                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new
                    {
                        Message = "Teacher ID not found in token."
                    });
                }

                if (!Guid.TryParse(userIdClaim, out Guid userId))
                {
                    return BadRequest(new
                    {
                        Message = "Invalid user ID format in token."
                    });
                }

                var response = await _courseService.GetCoursesByTeacherAsync(userId, request, status);

                if (response.Code == 404 || response.Data == null || !response.Data.Any())
                {
                    return NotFound(new
                    {
                        Message = "No courses found for this teacher.",
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = response.Meta?.TotalItems ?? 0
                    });
                }

                return StatusCode(response.Code, response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new
                {
                    Message = "Invalid status value provided.",
                    Details = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    Message = "An unexpected error occurred while retrieving courses.",
                    Details = ex.Message
                });
            }
        }
        /// <summary>
        /// Submit a course for staff review.
        /// </summary>
        /// <param name="courseId">The ID of the course to be submitted.</param>
        /// <returns>A response indicating whether the submission was successful.</returns>
        [HttpPost("{courseId:guid}/submit")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> SubmitCourseForReview(Guid courseId)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    Message = "Teacher ID not found in token."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    Message = "Invalid user ID format in token."
                });
            }

            var result = await _courseService.SubmitCourseForReviewAsync(userId, courseId);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Approve a course submission.
        /// </summary>
        /// <param name="submissionId">The ID of the submission to approve.</param>
        /// <returns>A response indicating whether the approval was successful.</returns>
        [HttpPut("submissions/{submissionId:guid}/approve")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> ApproveCourseSubmission(Guid submissionId)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    Message = "Teacher ID not found in token."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    Message = "Invalid user ID format in token."
                });
            }

            var result = await _courseService.ApproveCourseSubmissionAsync(userId, submissionId);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Reject a course submission with a feedback reason.
        /// </summary>
        /// <param name="submissionId">The ID of the submission to reject.</param>
        /// <param name="request">The reason for rejecting the submission.</param>
        /// <returns>A response indicating whether the rejection was successful.</returns>
        [HttpPut("submissions/{submissionId:guid}/reject")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> RejectCourseSubmission(Guid submissionId, [FromBody] RejectApplicationRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    Message = "Teacher ID not found in token."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    Message = "Invalid user ID format in token."
                });
            }

            var result = await _courseService.RejectCourseSubmissionAsync(userId, submissionId, request.Reason);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Get all course submissions assigned to the logged-in staff user.
        /// </summary>
        /// <param name="request">Paging parameters (page number and page size).</param>
        /// <param name="status">Filter by submission status (Pending, Approved, Rejected).</param>
        /// <returns>A paginated list of course submissions.</returns>
        [HttpGet("submissions/by-manager")]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> GetAllCourseSubmissionsByStaff([FromQuery] PagingRequest request, [FromQuery] string? status)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    Message = "Teacher ID not found in token."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    Message = "Invalid user ID format in token."
                });
            }

            var result = await _courseService.GetCourseSubmissionsByManagerAsync(userId, request, status);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Get all course submissions made by the logged-in teacher.
        /// </summary>
        /// <param name="request">Paging parameters (page number and page size).</param>
        /// <param name="status">Filter by submission status (Pending, Approved, Rejected).</param>
        /// <returns>A paginated list of course submissions.</returns>
        [HttpGet("submissions/by-teacher")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetAllCourseSubmissionsByTeacher([FromQuery] PagingRequest request, [FromQuery] string? status)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new
                {
                    Message = "Teacher ID not found in token."
                });
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest(new
                {
                    Message = "Invalid user ID format in token."
                });
            }
            var result = await _courseService.GetCourseSubmissionsByTeacherAsync(userId, request, status ?? "Pending");
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Lấy danh sách các khoá học phổ biến (public).
        /// </summary>
        /// <param name="count">Số lượng khoá học muốn lấy (mặc định 10, tối đa 50).</param>
        /// <returns>Danh sách khoá học phổ biến.</returns>
        [AllowAnonymous] // Cho phép tất cả mọi người truy cập
        [HttpGet("popular")]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<PopularCourseDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetPopularCourses(
            [FromQuery][Range(1, 50)] int count = 10)
        {
            var response = await _courseService.GetPopularCoursesAsync(count);


            return StatusCode(response.Code, response);
        }
    }
}

