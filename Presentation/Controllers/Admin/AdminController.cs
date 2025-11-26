using BLL.IServices.Admin;
using BLL.IServices.Auth;
using Common.DTO.Admin;
using Common.DTO.ApiResponse;
using Common.DTO.Auth;
using Common.DTO.Payment;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Presentation.Controllers.Admin
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Policy = "AdminOnly")]
    public class AdminController : ControllerBase
    {
        private readonly IAdminService _adminService;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminController> _logger;
        public AdminController(IAdminService adminService, IAuthService authService, ILogger<AdminController> logger)
        {
            _adminService = adminService;
            _authService = authService;
            _logger=logger;
        }

        /// <summary>
        /// Lấy thông tin dashboard tổng quan cho admin
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetAdminDashboard()
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var dashboard = await _adminService.GetAdminDashboardAsync(adminUserId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin dashboard thành công",
                    data = dashboard
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thông tin dashboard",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả người dùng
        /// </summary>
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var allUsers = await _adminService.GetAllUsersAsync(adminUserId);

                // Implement pagination
                var totalUsers = allUsers.Count;
                var users = allUsers
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách người dùng thành công",
                    data = new
                    {
                        users = users,
                        pagination = new
                        {
                            currentPage = page,
                            pageSize = pageSize,
                            totalUsers = totalUsers,
                            totalPages = (int)Math.Ceiling((double)totalUsers / pageSize)
                        }
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách người dùng",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy danh sách tất cả staff
        /// </summary>
        [HttpGet("staff")]
        public async Task<IActionResult> GetAllStaff()
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var staffList = await _adminService.GetAllStaffAsync(adminUserId);

                return Ok(new
                {
                    success = true,
                    message = "Lấy danh sách staff thành công",
                    data = staffList,
                    totalStaff = staffList.Count
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy danh sách staff",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Lấy thông tin chi tiết một người dùng
        /// </summary>
        [HttpGet("users/{userId:guid}")]
        public async Task<IActionResult> GetUserDetails(Guid userId)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var allUsers = await _adminService.GetAllUsersAsync(adminUserId);
                var user = allUsers.FirstOrDefault(u => u.UserID == userId);

                if (user == null)
                {
                    return NotFound(new
                    {
                        success = false,
                        message = "Không tìm thấy người dùng"
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Lấy thông tin người dùng thành công",
                    data = user
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thông tin người dùng",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Đổi mật khẩu cho staff
        /// </summary>
        [HttpPost("staff/change-password")]
        public async Task<IActionResult> ChangeStaffPassword([FromBody] ChangeStaffPasswordDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Dữ liệu không hợp lệ",
                        errors = ModelState
                    });
                }

                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var result = await _authService.ChangeStaffPasswordAsync(adminUserId, request);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Đổi mật khẩu staff thành công. Staff sẽ cần đăng nhập lại trên tất cả thiết bị."
                    });
                }

                return BadRequest(new
                {
                    success = false,
                    message = "Không thể đổi mật khẩu staff"
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi đổi mật khẩu staff",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Tìm kiếm người dùng theo từ khóa
        /// </summary>
        [HttpGet("users/search")]
        public async Task<IActionResult> SearchUsers([FromQuery] string? keyword, [FromQuery] string? role, [FromQuery] bool? status)
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var allUsers = await _adminService.GetAllUsersAsync(adminUserId);

                var filteredUsers = allUsers.AsQueryable();

                // Filter by keyword (username or email)
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    filteredUsers = filteredUsers.Where(u =>
                        u.UserName.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        u.Email.Contains(keyword, StringComparison.OrdinalIgnoreCase));
                }

                // Filter by role
                if (!string.IsNullOrWhiteSpace(role))
                {
                    filteredUsers = filteredUsers.Where(u =>
                        u.Roles.Any(r => r.Equals(role, StringComparison.OrdinalIgnoreCase)));
                }

                // Filter by status
                if (status.HasValue)
                {
                    filteredUsers = filteredUsers.Where(u => u.Status == status.Value);
                }

                var result = filteredUsers.ToList();

                return Ok(new
                {
                    success = true,
                    message = "Tìm kiếm người dùng thành công",
                    data = result,
                    totalFound = result.Count,
                    searchCriteria = new
                    {
                        keyword = keyword,
                        role = role,
                        status = status
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi tìm kiếm người dùng",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Thống kê người dùng theo vai trò
        /// </summary>
        [HttpGet("users/statistics/by-role")]
        public async Task<IActionResult> GetUserStatisticsByRole()
        {
            try
            {
                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var allUsers = await _adminService.GetAllUsersAsync(adminUserId);

                var roleStatistics = allUsers
                    .SelectMany(u => u.Roles)
                    .GroupBy(role => role)
                    .Select(g => new
                    {
                        Role = g.Key,
                        Count = g.Count(),
                        Percentage = Math.Round((double)g.Count() / allUsers.Count * 100, 2)
                    })
                    .OrderByDescending(x => x.Count)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = "Lấy thống kê người dùng theo vai trò thành công",
                    data = new
                    {
                        totalUsers = allUsers.Count,
                        roleStatistics = roleStatistics
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thống kê",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Thống kê người dùng theo thời gian đăng ký
        /// </summary>
        [HttpGet("users/statistics/by-registration-date")]
        public async Task<IActionResult> GetUserStatisticsByRegistrationDate([FromQuery] int days = 30)
        {
            try
            {
                if (days < 1 || days > 365) days = 30;

                var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var allUsers = await _adminService.GetAllUsersAsync(adminUserId);

                var cutoffDate = DateTime.UtcNow.AddDays(-days);
                var recentUsers = allUsers.Where(u => u.CreatedAt >= cutoffDate).ToList();

                var dailyRegistrations = recentUsers
                    .GroupBy(u => u.CreatedAt.Date)
                    .Select(g => new
                    {
                        Date = g.Key.ToString("yyyy-MM-dd"),
                        Count = g.Count()
                    })
                    .OrderBy(x => x.Date)
                    .ToList();

                return Ok(new
                {
                    success = true,
                    message = $"Lấy thống kê đăng ký {days} ngày gần đây thành công",
                    data = new
                    {
                        totalUsersInPeriod = recentUsers.Count,
                        dailyRegistrations = dailyRegistrations,
                        averagePerDay = Math.Round((double)recentUsers.Count / days, 2)
                    }
                });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "Đã xảy ra lỗi khi lấy thống kê đăng ký",
                    error = ex.Message
                });
            }
        }
        #region Program Endpoints

        [HttpGet("language/{languageId:guid}/programs")]
        public async Task<IActionResult> GetPrograms(Guid languageId)
        {
         
            var programs = await _adminService.GetProgramsByLanguageAsync(languageId);
            return Ok(new { success = true, data = programs });
        }

        [HttpGet("program/{programId:guid}")]
        public async Task<IActionResult> GetProgram(Guid programId)
        {
            var program = await _adminService.GetProgramByIdAsync(programId);
            if (program == null) return NotFound(new { success = false, message = "Không tìm thấy chương trình." });
            return Ok(new { success = true, data = program });
        }

        [HttpPost("program")]
        public async Task<IActionResult> CreateProgram([FromBody] ProgramCreateDto dto)
        {
            try
            {
                var program = await _adminService.CreateProgramAsync(dto);
                return CreatedAtAction(nameof(GetProgram), new { programId = program.ProgramId }, new { success = true, data = program });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo Program");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        [HttpPut("program/{programId:guid}")]
        public async Task<IActionResult> UpdateProgram(Guid programId, [FromBody] ProgramUpdateDto dto)
        {
            try
            {
                var program = await _adminService.UpdateProgramAsync(programId, dto);
                return Ok(new { success = true, data = program });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật Program {ProgramId}", programId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        [HttpDelete("program/{programId:guid}")]
        public async Task<IActionResult> DeleteProgram(Guid programId)
        {
            try
            {
                await _adminService.SoftDeleteProgramAsync(programId);
                return Ok(new { success = true, message = "Đã xóa (ẩn) chương trình thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex) // Lỗi do còn khóa học
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa Program {ProgramId}", programId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        #endregion

        #region Level Endpoints

        [HttpGet("program/{programId:guid}/levels")]
        public async Task<IActionResult> GetLevels(Guid programId)
        {
            var levels = await _adminService.GetLevelsByProgramAsync(programId);
            return Ok(new { success = true, data = levels });
        }

        [HttpGet("level/{levelId:guid}")]
        public async Task<IActionResult> GetLevel(Guid levelId)
        {
            var level = await _adminService.GetLevelByIdAsync(levelId);
            if (level == null) return NotFound(new { success = false, message = "Không tìm thấy cấp độ." });
            return Ok(new { success = true, data = level });
        }

        [HttpPost("level")]
        public async Task<IActionResult> CreateLevel([FromBody] LevelCreateDto dto)
        {
            try
            {
                var level = await _adminService.CreateLevelAsync(dto);
                return CreatedAtAction(nameof(GetLevel), new { levelId = level.LevelId }, new { success = true, data = level });
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi tạo Level");
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        [HttpPut("level/{levelId:guid}")]
        public async Task<IActionResult> UpdateLevel(Guid levelId, [FromBody] LevelUpdateDto dto)
        {
            try
            {
                var level = await _adminService.UpdateLevelAsync(levelId, dto);
                return Ok(new { success = true, data = level });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi cập nhật Level {LevelId}", levelId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }

        [HttpDelete("level/{levelId:guid}")]
        public async Task<IActionResult> DeleteLevel(Guid levelId)
        {
            try
            {
                await _adminService.SoftDeleteLevelAsync(levelId);
                return Ok(new { success = true, message = "Đã xóa (ẩn) cấp độ thành công." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { success = false, message = ex.Message });
            }
            catch (InvalidOperationException ex) // Lỗi do còn khóa học
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xóa Level {LevelId}", levelId);
                return StatusCode(500, new { success = false, message = "Đã xảy ra lỗi hệ thống." });
            }
        }
        [HttpGet("wallet")]
        public async Task<IActionResult> GetMyWallet()
        {
            try
            {

                var adminUserId = this.GetUserId();

                var wallet = await _adminService.GetAdminWalletAsync(adminUserId);

                return Ok(new BaseResponse<WalletDto>
                {
                    Status = "Success",
                    Message = "Lấy thông tin ví Admin thành công",
                    Data = wallet
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
        /// <summary>
        /// Khóa hoặc mở khóa người dùng (Giáo viên hoặc Học viên).
        /// Gửi email thông báo tự động.
        /// </summary>
        [HttpPost("users/ban")]
        public async Task<IActionResult> BanUser([FromBody] BanUserRequestDto request)
        {
            try
            {
               
                var adminIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out Guid adminId))
                {
                   
                    return Unauthorized(BaseResponse<object>.Fail(null, "Không xác định được danh tính Admin.", 401));
                }

             
                var newStatus = await _adminService.BanUserAsync(adminId, request.UserId, request.Reason);

              
                string statusMessage = newStatus ? "Đã mở khóa (Unban) thành công" : "Đã khóa (Ban) thành công";

                var responseData = new
                {
                    UserId = request.UserId,
                    IsActive = newStatus,
                    Action = newStatus ? "Unban" : "Ban"
                };

             
                return Ok(BaseResponse<object>.Success(responseData, $"{statusMessage}. Email thông báo đã được gửi."));
            }
            catch (KeyNotFoundException ex)
            {
               
                return NotFound(BaseResponse<object>.Fail(null, ex.Message, 404));
            }
            catch (InvalidOperationException ex)
            {
                
                return BadRequest(BaseResponse<object>.Fail(null, ex.Message, 400));
            }
            catch (Exception ex)
            {
            
                return StatusCode(500, BaseResponse<object>.Error(ex.Message));
            }
        }
    }
}

    #endregion



