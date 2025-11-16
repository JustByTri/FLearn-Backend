using BLL.IServices.Teacher;
using Common.DTO.ApiResponse;
using Common.DTO.PayOut;
using Common.DTO.Teacher;
using Common.DTO.Teacher.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.Net;
using System.Security.Claims;

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
            var response = await _teacherService.GetTeacherProfileWithWalletAsync(userId);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Tạo yêu cầu rút tiền (cho Teacher).
        /// </summary>
        /// <param name="requestDto">Thông tin số tiền và tài khoản ngân hàng.</param>
        [Authorize(Roles = "Teacher")]
        [HttpPost("payout-request")]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.BadRequest)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> CreatePayoutRequest([FromBody] CreatePayoutRequestDto requestDto)
        {
            if (!this.TryGetUserId(out Guid teacherId, out var errorResult))
            {
                return errorResult!;
            }

            var response = await _teacherService.CreatePayoutRequestAsync(teacherId, requestDto);
            return StatusCode(response.Code, response);
        }
        // <summary>
        /// (Teacher) Thêm một tài khoản ngân hàng mới.
        /// </summary>
        /// <param name="dto">Thông tin tài khoản ngân hàng.</param>
        [Authorize(Roles = "Teacher")]
        [HttpPost("bank-account")]
        [ProducesResponseType(typeof(BaseResponse<TeacherBankAccountDto>), (int)HttpStatusCode.Created)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.BadRequest)]
        public async Task<IActionResult> AddBankAccount([FromBody] CreateBankAccountDto dto)
        {
            if (!this.TryGetUserId(out Guid teacherId, out var errorResult))
            {
                return errorResult!;
            }

            var response = await _teacherService.AddBankAccountAsync(teacherId, dto);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// (Teacher) Lấy danh sách tài khoản ngân hàng đã thêm.
        /// </summary>
        [Authorize(Roles = "Teacher")]
        [HttpGet("bank-account")] // Dùng HTTP GET
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<TeacherBankAccountDto>>), (int)HttpStatusCode.OK)]
        public async Task<IActionResult> GetMyBankAccounts()
        {
            if (!this.TryGetUserId(out Guid teacherId, out var errorResult))
            {
                return errorResult!;
            }

            var response = await _teacherService.GetMyBankAccountsAsync(teacherId);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// (Public) Lấy hồ sơ công khai của giáo viên.
        /// </summary>
        /// <param name="teacherId">ID của giáo viên (TeacherId).</param>
        [AllowAnonymous]
        [HttpGet("{teacherId}/profile")] 
        [ProducesResponseType(typeof(BaseResponse<PublicTeacherProfileDto>), (int)HttpStatusCode.OK)]
        [ProducesResponseType(typeof(BaseResponse<object>), (int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetPublicProfile([FromRoute] Guid teacherId) 
        {
            var response = await _teacherService.GetPublicTeacherProfileAsync(teacherId);
            return StatusCode(response.Code, response);
        }
        [HttpGet("teaching-programs")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> GetTeachingPrograms([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            var response = await _teacherService.GetTeachingProgramAsync(userId, pageNumber, pageSize);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Tìm kiếm/lọc danh sách lớp của giáo viên
        /// </summary>
        [Authorize(Roles = "Learner")]
        [HttpGet("classes/search")]
        public async Task<IActionResult> SearchClasses([FromQuery] string? keyword = null, [FromQuery] string? status = null, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null, [FromQuery] Guid? programId = null, [FromQuery] Guid? teacherId = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            if (!this.TryGetUserId(out Guid currentTeacherId, out var errorResult))
                return errorResult!;
            var response = await _teacherService.SearchClassesAsync(teacherId ?? currentTeacherId, keyword, status, from, to, programId, page, pageSize);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Public: Search lớp học theo nhiều tiêu chí (không cần auth)
        /// </summary>
        [AllowAnonymous]
        [HttpGet("classes/public/search")]
        public async Task<IActionResult> PublicSearchClasses(
            [FromQuery] Guid? languageId = null,
            [FromQuery] Guid? teacherId = null,
            [FromQuery] Guid? programId = null,
            [FromQuery] string? keyword = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? from = null,
            [FromQuery] DateTime? to = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var response = await _teacherService.PublicSearchClassesAsync(languageId, teacherId, programId, keyword, status, from, to, page, pageSize);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Lấy tất cả giáo viên
        /// </summary>
        [HttpGet("teachers/all")]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllTeachers()
        {
            var response = await _teacherService.GetAllTeachersAsync();
            return StatusCode(response.Code, response);
        }
    }
}
