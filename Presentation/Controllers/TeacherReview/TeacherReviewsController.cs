using BLL.IServices.AI;
using BLL.IServices.TeacherReview;
using Common.DTO.Paging.Request;
using Common.DTO.TeacherReview.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.TeacherReview
{
    [Route("api/teacher-reviews")]
    [ApiController]
    public class TeacherReviewsController : ControllerBase
    {
        private readonly ITeacherReviewService _teacherReviewService;
        private readonly IAIContentModerationService _moderationService;
        public TeacherReviewsController(ITeacherReviewService teacherReviewService, IAIContentModerationService moderationService)
        {
            _teacherReviewService = teacherReviewService;
            _moderationService = moderationService;
        }
        [HttpGet("teachers/{teacherId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsByTeacherId(Guid teacherId, [FromQuery] PaginationParams paginationParams)
        {
            var response = await _teacherReviewService.GetTeacherReviewsByTeacherIdAsync(teacherId, paginationParams);
            return StatusCode(response.Code, response);
        }
        [HttpPost("teachers/{teacherId}")]
        [Authorize]
        public async Task<IActionResult> CreateReview(Guid teacherId, [FromBody] TeacherReviewRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isSafe = await _moderationService.IsContentSafeAsync(request.Comment);
            if (!isSafe)
            {
                return BadRequest(new { message = "Đánh giá của bạn có chứa nội dung không phù hợp, vi phạm tiêu chuẩn cộng đồng của chúng tôi (Bạo lực, Lời nói kích động thù địch, Phản động chính trị, v.v.) và đã bị từ chối." });
            }

            var response = await _teacherReviewService.CreateTeacherReviewAsync(userId, teacherId, request);
            return StatusCode(response.Code, response);
        }
        [HttpPut("teachers/{teacherId}")]
        [Authorize]
        public async Task<IActionResult> UpdateReview(Guid teacherId, [FromBody] TeacherReviewRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var isSafe = await _moderationService.IsContentSafeAsync(request.Comment);
            if (!isSafe)
            {
                return BadRequest(new { message = "Đánh giá của bạn có chứa nội dung không phù hợp, vi phạm tiêu chuẩn cộng đồng của chúng tôi (Bạo lực, Lời nói kích động thù địch, Phản động chính trị, v.v.) và đã bị từ chối." });
            }

            var response = await _teacherReviewService.UpdateTeacherReviewAsync(userId, teacherId, request);
            return StatusCode(response.Code, response);
        }
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteReview(Guid id)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var response = await _teacherReviewService.DeleteTeacherReviewAsync(userId, id);
            return StatusCode(response.Code, response);
        }
    }
}
