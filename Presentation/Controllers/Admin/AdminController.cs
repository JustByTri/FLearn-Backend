using BLL.IServices.Admin;
using BLL.IServices.Auth;
using Common.DTO.Admin;
using Common.DTO.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

        public AdminController(IAdminService adminService, IAuthService authService)
        {
            _adminService = adminService;
            _authService = authService;
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

     
    }
}

