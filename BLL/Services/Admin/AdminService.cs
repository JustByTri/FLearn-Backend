using BLL.IServices.Admin;
using BLL.IServices.Auth;
using BLL.Settings;
using Common.DTO.Admin;
using DAL.UnitOfWork;
using Microsoft.Extensions.Options;

namespace BLL.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;

        public AdminService(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings, IEmailService emailService, IAuthService authService)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _authService = authService;
        }

        public async Task<List<UserListDto>> GetAllUsersAsync(Guid adminUserId)
        {

            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xem danh sách người dùng");
            }

            var users = await _unitOfWork.Users.GetAllUsersWithRolesAsync();

            return users.Select(user => new UserListDto
            {
                UserID = user.UserID,
                UserName = user.UserName,
                Email = user.Email,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                IsEmailConfirmed = user.IsEmailConfirmed,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            }).ToList();
        }

        public async Task<List<UserListDto>> GetAllStaffAsync(Guid adminUserId)
        {

            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xem danh sách staff");
            }

            var staffUsers = await _unitOfWork.Users.GetUsersByRoleAsync("Staff");

            return staffUsers.Select(user => new UserListDto
            {
                UserID = user.UserID,
                UserName = user.UserName,
                Email = user.Email,
                Status = user.Status,
                CreatedAt = user.CreatedAt,
                IsEmailConfirmed = user.IsEmailConfirmed,
                Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
            }).ToList();
        }

        public async Task<AdminDashboardDto> GetAdminDashboardAsync(Guid adminUserId)
        {

            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xem dashboard");
            }

            var totalUsers = await _unitOfWork.Users.GetTotalUsersCountAsync();
            var totalStaff = await _unitOfWork.Users.GetUsersCountByRoleAsync("Staff");
            var activeUsers = await _unitOfWork.Users.GetActiveUsersCountAsync();
            var recentUsers = await _unitOfWork.Users.GetRecentUsersAsync(5);

            return new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalStaff = totalStaff,
                ActiveUsers = activeUsers,
                RecentUsers = recentUsers.Select(user => new UserListDto
                {
                    UserID = user.UserID,
                    UserName = user.UserName,
                    Email = user.Email,
                    Status = user.Status,
                    CreatedAt = user.CreatedAt,
                    IsEmailConfirmed = user.IsEmailConfirmed,
                    Roles = user.UserRoles?.Select(ur => ur.Role.Name).ToList() ?? new List<string>()
                }).ToList()
            };
        }
    }
}

