using BLL.IServices.Admin;
using BLL.IServices.Auth;
using BLL.Settings;
using Common.DTO.Admin;
using DAL.Models;
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
        public async Task<List<GlobalConversationPromptDto>> GetAllGlobalPromptsAsync(Guid adminUserId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể quản lý global prompts");
            }

            var prompts = await _unitOfWork.GlobalConversationPrompts.GetAllAsync();

            return prompts.Select(p => new GlobalConversationPromptDto
            {
                GlobalPromptID = p.GlobalPromptID,
                PromptName = p.PromptName,
                Description = p.Description,
                MasterPromptTemplate = p.MasterPromptTemplate,
                ScenarioGuidelines = p.ScenarioGuidelines,
                RoleplayInstructions = p.RoleplayInstructions,
                EvaluationCriteria = p.EvaluationCriteria,
                IsActive = p.IsActive,
                IsDefault = p.IsDefault,
                UsageCount = p.UsageCount,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            }).OrderByDescending(p => p.IsDefault).ThenBy(p => p.PromptName).ToList();
        }

        public async Task<GlobalConversationPromptDto> CreateGlobalPromptAsync(Guid adminUserId, CreateGlobalPromptDto createPromptDto)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể tạo global prompt");
            }

            if (createPromptDto.IsDefault)
            {
                await _unitOfWork.GlobalConversationPrompts.SetAsDefaultAsync(Guid.Empty);
            }

            var newPrompt = new GlobalConversationPrompt
            {
                GlobalPromptID = Guid.NewGuid(),
                PromptName = createPromptDto.PromptName,
                Description = createPromptDto.Description,
                MasterPromptTemplate = createPromptDto.MasterPromptTemplate,
                ScenarioGuidelines = createPromptDto.ScenarioGuidelines,
                RoleplayInstructions = createPromptDto.RoleplayInstructions,
                EvaluationCriteria = createPromptDto.EvaluationCriteria,
                IsActive = createPromptDto.IsActive,
                LanguageID = createPromptDto.LanguageID,
                IsDefault = createPromptDto.IsDefault,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GlobalConversationPrompts.CreateAsync(newPrompt);
            await _unitOfWork.SaveChangesAsync();

            return new GlobalConversationPromptDto
            {
                GlobalPromptID = newPrompt.GlobalPromptID,
                PromptName = newPrompt.PromptName,
                Description = newPrompt.Description,
                MasterPromptTemplate = newPrompt.MasterPromptTemplate,
                ScenarioGuidelines = newPrompt.ScenarioGuidelines,
                RoleplayInstructions = newPrompt.RoleplayInstructions,
                EvaluationCriteria = newPrompt.EvaluationCriteria,
                IsActive = newPrompt.IsActive,
                IsDefault = newPrompt.IsDefault,
                UsageCount = newPrompt.UsageCount,
                CreatedAt = newPrompt.CreatedAt,
                UpdatedAt = newPrompt.UpdatedAt
            };
        }

        public async Task<GlobalConversationPromptDto> UpdateGlobalPromptAsync(Guid adminUserId, Guid promptId, UpdateGlobalPromptDto updatePromptDto)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể cập nhật global prompt");
            }

            var existingPrompt = await _unitOfWork.GlobalConversationPrompts.GetByIdAsync(promptId);
            if (existingPrompt == null)
            {
                throw new KeyNotFoundException("Không tìm thấy global prompt");
            }

            if (updatePromptDto.IsDefault && !existingPrompt.IsDefault)
            {
                await _unitOfWork.GlobalConversationPrompts.SetAsDefaultAsync(promptId);
            }

            existingPrompt.PromptName = updatePromptDto.PromptName;
            existingPrompt.Description = updatePromptDto.Description;
            existingPrompt.MasterPromptTemplate = updatePromptDto.MasterPromptTemplate;
            existingPrompt.ScenarioGuidelines = updatePromptDto.ScenarioGuidelines;
            existingPrompt.RoleplayInstructions = updatePromptDto.RoleplayInstructions;
            existingPrompt.EvaluationCriteria = updatePromptDto.EvaluationCriteria;
            existingPrompt.IsActive = updatePromptDto.IsActive;
            existingPrompt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.GlobalConversationPrompts.UpdateAsync(existingPrompt);
            await _unitOfWork.SaveChangesAsync();

            return new GlobalConversationPromptDto
            {
                GlobalPromptID = existingPrompt.GlobalPromptID,
                PromptName = existingPrompt.PromptName,
                Description = existingPrompt.Description,
                MasterPromptTemplate = existingPrompt.MasterPromptTemplate,
                ScenarioGuidelines = existingPrompt.ScenarioGuidelines,
                RoleplayInstructions = existingPrompt.RoleplayInstructions,
                EvaluationCriteria = existingPrompt.EvaluationCriteria,
                IsActive = existingPrompt.IsActive,
                IsDefault = existingPrompt.IsDefault,
                UsageCount = existingPrompt.UsageCount,
                CreatedAt = existingPrompt.CreatedAt,
                UpdatedAt = existingPrompt.UpdatedAt
            };
        }

        public async Task<bool> DeleteGlobalPromptAsync(Guid adminUserId, Guid promptId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xóa global prompt");
            }

            var existingPrompt = await _unitOfWork.GlobalConversationPrompts.GetByIdAsync(promptId);
            if (existingPrompt == null)
            {
                throw new KeyNotFoundException("Không tìm thấy global prompt");
            }

            if (existingPrompt.UsageCount > 0)
            {
                throw new InvalidOperationException("Không thể xóa prompt đã được sử dụng");
            }

            await _unitOfWork.GlobalConversationPrompts.RemoveAsync(existingPrompt);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<bool> ToggleGlobalPromptStatusAsync(Guid adminUserId, Guid promptId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể thay đổi trạng thái global prompt");
            }

            var existingPrompt = await _unitOfWork.GlobalConversationPrompts.GetByIdAsync(promptId);
            if (existingPrompt == null)
            {
                throw new KeyNotFoundException("Không tìm thấy global prompt");
            }

            existingPrompt.IsActive = !existingPrompt.IsActive;
            existingPrompt.UpdatedAt = DateTime.UtcNow;

            await _unitOfWork.GlobalConversationPrompts.UpdateAsync(existingPrompt);
            await _unitOfWork.SaveChangesAsync();

            return true;
        }

        public async Task<GlobalConversationPromptDto> GetActiveGlobalPromptAsync(Guid adminUserId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xem global prompts");
            }

            var activePrompt = await _unitOfWork.GlobalConversationPrompts.GetActiveDefaultPromptAsync();

            if (activePrompt == null)
            {
                throw new InvalidOperationException("Không tìm thấy global prompt nào đang active");
            }

            return new GlobalConversationPromptDto
            {
                GlobalPromptID = activePrompt.GlobalPromptID,
                PromptName = activePrompt.PromptName,
                Description = activePrompt.Description,
                MasterPromptTemplate = activePrompt.MasterPromptTemplate,
                ScenarioGuidelines = activePrompt.ScenarioGuidelines,
                RoleplayInstructions = activePrompt.RoleplayInstructions,
                EvaluationCriteria = activePrompt.EvaluationCriteria,
                IsActive = activePrompt.IsActive,
                IsDefault = activePrompt.IsDefault,
                UsageCount = activePrompt.UsageCount,
                CreatedAt = activePrompt.CreatedAt,
                UpdatedAt = activePrompt.UpdatedAt
            };
        }

        public async Task<GlobalConversationPromptDto> GetGlobalPromptByIdAsync(Guid adminUserId, Guid promptId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xem global prompts");
            }

            var prompt = await _unitOfWork.GlobalConversationPrompts.GetByIdAsync(promptId);
            if (prompt == null)
            {
                throw new KeyNotFoundException("Không tìm thấy global prompt");
            }

            return new GlobalConversationPromptDto
            {
                GlobalPromptID = prompt.GlobalPromptID,
                PromptName = prompt.PromptName,
                Description = prompt.Description,
                MasterPromptTemplate = prompt.MasterPromptTemplate,
                ScenarioGuidelines = prompt.ScenarioGuidelines,
                RoleplayInstructions = prompt.RoleplayInstructions,
                EvaluationCriteria = prompt.EvaluationCriteria,
                IsActive = prompt.IsActive,
                IsDefault = prompt.IsDefault,
                UsageCount = prompt.UsageCount,
                CreatedAt = prompt.CreatedAt,
                UpdatedAt = prompt.UpdatedAt
            };
        }
    }
}


