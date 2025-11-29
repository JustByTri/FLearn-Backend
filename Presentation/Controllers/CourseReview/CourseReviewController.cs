using BLL.IServices.Course;
using Common.DTO.CourseReview.Request;
using Common.DTO.Paging.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.CourseReview
{
    [Route("api/course-reviews")]
    [ApiController]
    public class CourseReviewController : ControllerBase
    {
        private readonly ICourseReviewService _courseReviewService;

        public CourseReviewController(ICourseReviewService courseReviewService)
        {
            _courseReviewService = courseReviewService;
        }
        [HttpGet("courses/{courseId:guid}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByCourseId(Guid courseId, [FromQuery] PaginationParams @params)
        {
            var result = await _courseReviewService.GetCourseReviewsByCourseIdAsync(courseId, @params);
            return StatusCode(result.Code, result);
        }
        [HttpPost("courses/{courseId:guid}")]
        [Authorize]
        public async Task<IActionResult> CreateReview(Guid courseId, [FromBody] CourseReviewRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseReviewService.CreateCourseReviewAsync(userId, courseId, request);
            return StatusCode(result.Code, result);
        }
        [HttpPut("courses/{courseId:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(Guid courseId, [FromBody] CourseReviewRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseReviewService.UpdateCourseReviewAsync(userId, courseId, request);
            return StatusCode(result.Code, result);
        }
        [HttpDelete("{reviewId:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid reviewId)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _courseReviewService.DeleteCourseReviewAsync(userId, reviewId);
            return StatusCode(result.Code, result);
        }
    }
}
