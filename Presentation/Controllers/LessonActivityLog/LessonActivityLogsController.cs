using BLL.IServices.LessonActivityLog;
using Common.DTO.LessonLog.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.LessonActivityLog
{
    [ApiController]
    [Route("api/lesson-logs")]
    [Authorize]
    public class LessonActivityLogsController : ControllerBase
    {
        private readonly ILessonActivityLogService _service;
        public LessonActivityLogsController(ILessonActivityLogService service)
        {
            _service = service;
        }
        [HttpPost]
        public async Task<IActionResult> AddLog([FromBody] LessonLogRequest req)
        {
            var userIdClaim = User.FindFirstValue("user_id") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized("Teacher ID not found in token.");

            if (!Guid.TryParse(userIdClaim, out Guid userId))
                return BadRequest("Invalid user ID format in token.");
            var result = await _service.AddLogAsync(userId, req);
            return StatusCode(result.Code, result);
        }
    }
}
