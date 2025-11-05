// Presentation/Controllers/Teacher/TeacherClassController.cs
using BLL.IServices.Teacher;
using Common.DTO.Teacher;
using DAL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.Teacher
{
    [Route("api/teacher/classes")]
    [ApiController]
    [Authorize(Policy = "TeacherOnly")]
    public class TeacherClassController : ControllerBase
    {
        private readonly ITeacherClassService _teacherClassService;

        public TeacherClassController(ITeacherClassService teacherClassService)
        {
            _teacherClassService = teacherClassService;
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
                    data = result
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
    }
}
