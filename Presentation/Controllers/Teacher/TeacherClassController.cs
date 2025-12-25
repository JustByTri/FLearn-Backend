// Presentation/Controllers/Teacher/TeacherClassController.cs
using BLL.IServices.Teacher;
using Common.DTO.Teacher;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.Security.Claims;

namespace Presentation.Controllers.Teacher
{
    [Route("api/teacher/classes")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherClassController : ControllerBase
    {
        private readonly ITeacherClassService _teacherClassService;
        private readonly ILogger<TeacherClassController> _logger;

        public TeacherClassController(ITeacherClassService teacherClassService,ILogger<TeacherClassController> logger)
        {
            _teacherClassService = teacherClassService;
            _logger = logger;
        }

        /// <summary>
        /// Tạo lớp học mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateClass([FromBody] CreateClassDto createClassDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _teacherClassService.CreateClassAsync(teacherId, createClassDto);

                return CreatedAtAction(nameof(GetClassDetails), new { classId = result.ClassID }, new
                {
                    success = true,
                    message = "Tạo lớp học thành công",
                    data = new
                    {
                        classId = result.ClassID,
                        title = result.Title,
                        description = result.Description,
                        classDate = result.StartDateTime.Date,
                        startTime = result.StartDateTime.ToString("HH:mm:ss"),
                        durationMinutes = (int)(result.EndDateTime - result.StartDateTime).TotalMinutes,
                        pricePerStudent = result.PricePerStudent,
                        minStudents = result.MinStudents,
                        capacity = result.Capacity,
                        programAssignmentId = createClassDto.ProgramAssignmentId,
                        googleMeetLink = result.GoogleMeetLink
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi tạo lớp học" });
            }
        }

        /// <summary>
        /// Publish lớp học để học sinh có thể đăng ký
        /// </summary>
        [HttpPost("{classId:guid}/publish")]
        public async Task<IActionResult> PublishClass(Guid classId)
        {
            try
            {
                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _teacherClassService.PublishClassAsync(teacherId, classId);

                return Ok(new
                {
                    success = true,
                    message = "Lớp học đã được công bố thành công. Học sinh giờ đây có thể đăng ký."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi publish lớp học" });
            }
        }

        /// <summary>
        /// Lấy danh sách lớp học của giáo viên
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetMyClasses([FromQuery] string? status = null)
        {
            try
            {
                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

                ClassStatus? classStatus = null;
                if (!string.IsNullOrEmpty(status) && Enum.TryParse<ClassStatus>(status, true, out var parsedStatus))
                {
                    classStatus = parsedStatus;
                }

                var classes = await _teacherClassService.GetTeacherClassesAsync(teacherId, classStatus);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách lớp học thành công",
                    data = classes,
                    total = classes.Count,
                    statistics = new
                    {
                        totalClasses = classes.Count,
                        publishedClasses = classes.Count(c => c.Status == "Published"),
                        draftClasses = classes.Count(c => c.Status == "Draft"),
                        completedClasses = classes.Count(c => c.Status.Contains("Completed"))
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách lớp học" });
            }
        }

        /// <summary>
        /// Lấy chi tiết lớp học
        /// </summary>
        [HttpGet("{classId:guid}")]
        public async Task<IActionResult> GetClassDetails(Guid classId)
        {
            try
            {
                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var classDetails = await _teacherClassService.GetClassDetailsAsync(teacherId, classId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin lớp học thành công",
                    data = classDetails
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy thông tin lớp học" });
            }
        }
        /// <summary>
        /// Cập nhật thông tin lớp học
        /// - Chỉ cho phép cập nhật lớp ở trạng thái Draft hoặc Scheduled
        /// - Nếu đã có học sinh đăng ký, không thể thay đổi MinStudents, Capacity, PricePerStudent
        /// - Cho phép cập nhật link Google Meet
        /// </summary>
        [HttpPut("{classId:guid}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> UpdateClass(Guid classId, [FromBody] UpdateClassDto updateClassDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Dữ liệu không hợp lệ", errors = ModelState });
                }

                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _teacherClassService.UpdateClassAsync(teacherId, classId, updateClassDto);

                return Ok(new
                {
                    success = true,
                    message = "Cập nhật lớp học thành công",
                    data = result
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi cập nhật lớp học" });
            }
        }
        /// <summary>
        /// Lấy danh sách học sinh đã đăng ký lớp học
        /// </summary>
        [HttpGet("{classId:guid}/enrollments")]
        public async Task<IActionResult> GetClassEnrollments(Guid classId)
        {
            try
            {
                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var enrollments = await _teacherClassService.GetClassEnrollmentsAsync(teacherId, classId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách học sinh thành công",
                    data = enrollments,
                    total = enrollments.Count,
                    statistics = new
                    {
                        totalEnrollments = enrollments.Count,
                        paidEnrollments = enrollments.Count(e => e.Status == "Paid"),
                        pendingEnrollments = enrollments.Count(e => e.Status == "Pending")
                    }
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách học sinh" });
            }
        }

        /// <summary>
        /// Danh sách Program Assignment của giáo viên (để chọn khi tạo class)
        /// </summary>
        [HttpGet("assignments")]
        public async Task<IActionResult> GetMyAssignments()
        {
            try
            {
                var teacherId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var items = await _teacherClassService.GetMyProgramAssignmentsAsync(teacherId);
                return Ok(new { success = true, data = items, total = items.Count });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception)
            {
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi khi lấy danh sách assignment" });
            }
        }
        /// <summary>
        /// [Teacher] Hủy lớp học
        /// - Nếu > 3 ngày trước khi bắt đầu: Hủy trực tiếp
        /// - Nếu ≤ 3 ngày: Throw error yêu cầu dùng endpoint /request-cancel
        /// </summary>
        /// <param name="classId">ID lớp học cần hủy</param>
        /// <param name="dto">Lý do hủy lớp</param>
        [HttpDelete("{classId:guid}")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> CancelClass(
            Guid classId,
            [FromBody] CancelClassRequestDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var teacherId, out var error))
                    return error!;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _teacherClassService.CancelClassAsync(
                    teacherId,
                    classId,
                    dto.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Lớp học đã được hủy thành công. Hệ thống sẽ tự động tạo đơn hoàn tiền cho học viên."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                // Có thể là lỗi "không thể hủy trong vòng 3 ngày"
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    hint = "Vui lòng sử dụng chức năng 'Yêu cầu hủy lớp' thay vì hủy trực tiếp."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling class {ClassId}", classId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Teacher] Gửi yêu cầu hủy lớp (cho trường hợp < 3 ngày trước khi bắt đầu)
        /// </summary>
        /// <param name="classId">ID lớp học</param>
        /// <param name="dto">Lý do yêu cầu hủy</param>
        [HttpPost("{classId:guid}/request-cancel")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> RequestCancelClass(
            Guid classId,
            [FromBody] CancelClassRequestDto dto)
        {
            try
            {
                if (!this.TryGetUserId(out var teacherId, out var error))
                    return error!;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var requestId = await _teacherClassService.RequestCancelClassAsync(
                    teacherId,
                    classId,
                    dto.Reason);

                return Ok(new
                {
                    success = true,
                    message = "Yêu cầu hủy lớp đã được gửi. Manager sẽ xem xét trong thời gian sớm nhất.",
                    requestId = requestId
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error requesting class cancellation for {ClassId}", classId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }

        /// <summary>
        /// [Teacher] Xóa lớp học ở trạng thái Draft
        /// </summary>
        [HttpDelete("{classId:guid}/draft")]
        [Authorize(Roles = "Teacher")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(typeof(object), 400)]
        [ProducesResponseType(typeof(object), 404)]
        public async Task<IActionResult> DeleteDraftClass(Guid classId)
        {
            try
            {
                if (!this.TryGetUserId(out var teacherId, out var error))
                    return error!;

                var result = await _teacherClassService.DeleteClassAsync(teacherId, classId);
                return Ok(new
                {
                    success = true,
                    message = "Lớp học ở trạng thái Draft đã được xóa thành công."
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting draft class {ClassId}", classId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống" });
            }
        }
    }
}

