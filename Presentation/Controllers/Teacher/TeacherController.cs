using BLL.IServices.Teacher;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;

namespace Presentation.Controllers.Teacher
{
    [Route("api")]
    [ApiController]
    public class TeacherController : ControllerBase
    {
        private readonly ITeacherService _teacherService;
        public TeacherController(ITeacherService teacherService)
        {
            _teacherService = teacherService;
        }

        [Authorize(Roles = "Teacher")]
        [HttpGet("teachers/profile")]
        public async Task<IActionResult> GetTeacherProfile()
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var response = await _teacherService.GetTeacherProfileAsync(userId);
            return StatusCode(response.Code, response);
        }
    }
}
