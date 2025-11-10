using BLL.IServices.Enrollment;
using BLL.IServices.Purchases;
using Common.DTO.ApiResponse;
using Common.DTO.Enrollment.Request;
using Common.DTO.Language;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Enrollment
{
    [Route("api/enrollments")]
    [ApiController]
    public class EnrollmentController : ControllerBase
    {
        private readonly IEnrollmentService _enrollmentService;
        private readonly IPurchaseService _purchaseService;
        public EnrollmentController(IEnrollmentService enrollmentService, IPurchaseService purchaseService)
        {
            _enrollmentService = enrollmentService;
            _purchaseService = purchaseService;
        }
        /// <summary>
        /// Enrolls the authenticated user into a specified course.
        /// </summary>
        /// <param name="request">The enrollment request containing the course ID.</param>
        /// <returns>
        /// A <see cref="BaseResponse{EnrollmentResponse}"/> containing the enrollment details
        /// if successful, or an error message otherwise.
        /// </returns>
        /// <response code="200">Successfully enrolled in the course.</response>
        /// <response code="400">User is already enrolled in the course or invalid data provided.</response>
        /// <response code="401">Unauthorized request (user not authenticated).</response>
        /// <response code="403">Access denied (account inactive, email not confirmed, or course not purchased).</response>
        /// <response code="404">The specified course was not found.</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize(Roles = "Learner")]
        [HttpPost]
        public async Task<IActionResult> EnrollInCourseAsync([FromBody] EnrollmentRequest request)
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

            var result = await _enrollmentService.EnrolCourseAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Retrieves all courses that the authenticated user has enrolled in, with pagination.
        /// </summary>
        /// <returns>
        /// A paged list of enrolled courses wrapped in <see cref="PagedResponse{EnrollmentResponse}"/>.
        /// </returns>
        /// <response code="200">Successfully retrieved enrolled courses.</response>
        /// <response code="401">Unauthorized request (user not authenticated).</response>
        /// <response code="500">Internal server error occurred.</response>
        [Authorize(Roles = "Learner")]
        [HttpGet]
        public async Task<IActionResult> GetEnrolledCoursesAsync([FromQuery] PagingRequest request, [FromQuery][AllowedLang] string lang)
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

            var result = await _enrollmentService.GetEnrolledCoursesAsync(userId, lang, request);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("courses/{courseId}/access")]
        public async Task<IActionResult> CheckCourseAccess(Guid courseId)
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

            var result = await _purchaseService.CheckCourseAccessAsync(userId, courseId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyEnrolledCourses([FromQuery] PagingRequest request)
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

            var result = await _enrollmentService.GetEnrolledCoursesOverviewAsync(userId, request);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("{enrollmentId:guid}/details")]
        public async Task<IActionResult> GetEnrolledCourseDetails(Guid enrollmentId)
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

            var result = await _enrollmentService.GetEnrolledCourseDetailAsync(userId, enrollmentId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("{enrollmentId:guid}/curriculums")]
        public async Task<IActionResult> GetEnrolledCourseCurriculum(Guid enrollmentId)
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

            var result = await _enrollmentService.GetEnrolledCourseCurriculumAsync(userId, enrollmentId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpGet("continue-learning")]
        public async Task<IActionResult> GetContinueLearning()
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

            var result = await _enrollmentService.GetContinueLearningAsync(userId);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Learner")]
        [HttpPost("{enrollmentId:guid}/resume")]
        public async Task<IActionResult> ResumeCourseBySpecificLesson(Guid enrollmentId, [FromBody] ResumeCourseRequest request)
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

            var result = await _enrollmentService.ResumeCourseAsync(userId, enrollmentId, request);
            return StatusCode(result.Code, result);
        }
    }
}
