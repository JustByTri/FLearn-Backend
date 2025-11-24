using BLL.IServices.Admin;
using BLL.IServices.Auth;
using BLL.Settings;
using Common.DTO.Admin;
using DAL.Helpers;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BLL.Services.Admin
{
    public class AdminService : IAdminService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly JwtSettings _jwtSettings;
        private readonly IEmailService _emailService;
        private readonly IAuthService _authService;
        private readonly ILogger<AdminService> _logger;
        public AdminService(IUnitOfWork unitOfWork, IOptions<JwtSettings> jwtSettings, IEmailService emailService, IAuthService authService, ILogger<AdminService> logger)
        {
            _unitOfWork = unitOfWork;
            _jwtSettings = jwtSettings.Value;
            _emailService = emailService;
            _authService = authService;
            _logger = logger;
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
                throw new UnauthorizedAccessException("Chỉ admin mới có thể xem danh sách manager");
            }

            var staffUsers = await _unitOfWork.Users.GetUsersByRoleAsync("Manager");

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
            var totalStaff = await _unitOfWork.Users.GetUsersCountByRoleAsync("Manager");
            var activeUsers = await _unitOfWork.Users.GetActiveUsersCountAsync();
            var recentUsers = await _unitOfWork.Users.GetRecentUsersAsync(5);
            var totalCourses = await _unitOfWork.Courses.GetAllAsync();
            var refundResquest = await _unitOfWork.RefundRequests.GetPendingCountAsync();

            return new AdminDashboardDto
            {
                TotalUsers = totalUsers,
                TotalStaff = totalStaff,
                ActiveUsers = activeUsers,
                TotalCourses = totalCourses.Count(),
             PendingRequest = refundResquest,
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

        public async Task<GlobalConversationPromptDto> CreateGlobalPromptAsync(
        Guid adminUserId,
        CreateGlobalPromptDto createPromptDto)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Only admins can create global prompts");
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

                Status = "Draft",
                IsActive = false,
                IsDefault = false,

                CreatedByAdminId = adminUserId,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.GlobalConversationPrompts.CreateAsync(newPrompt);
            await _unitOfWork.SaveChangesAsync();



            return MapToDto(newPrompt);
        }
        public async Task<GlobalConversationPromptDto> ActivateGlobalPromptAsync(
    Guid adminUserId,
    Guid promptId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Only admins can activate prompts");
            }

            var prompt = await _unitOfWork.GlobalConversationPrompts.GetByIdAsync(promptId);
            if (prompt == null)
                throw new KeyNotFoundException("Prompt not found");

            if (prompt.Status != "Draft" && prompt.Status != "Review")
                throw new InvalidOperationException("Only Draft or Review prompts can be activated");

            // ✅ Deactivate tất cả prompts cũ
            var activePrompts = await _unitOfWork.GlobalConversationPrompts.GetActivePromptsAsync();
            foreach (var activePrompt in activePrompts)
            {
                activePrompt.IsActive = false;
                activePrompt.Status = "Archived";
                activePrompt.UpdatedAt = TimeHelper.GetVietnamTime(); ;
                await _unitOfWork.GlobalConversationPrompts.UpdateAsync(activePrompt);
            }

            // ✅ Activate prompt mới
            prompt.IsActive = true;
            prompt.Status = "Active";
            prompt.IsDefault = true;
            prompt.LastModifiedByAdminId = adminUserId;
            prompt.UpdatedAt = TimeHelper.GetVietnamTime(); ;

            await _unitOfWork.GlobalConversationPrompts.UpdateAsync(prompt);
            await _unitOfWork.SaveChangesAsync();



            return MapToDto(prompt);
        }
        public async Task<GlobalConversationPromptDto> GetActiveGlobalPromptAsync(Guid adminUserId)
        {
            var adminUser = await _unitOfWork.Users.GetUserWithRolesAsync(adminUserId);
            if (adminUser == null || !adminUser.UserRoles.Any(ur => ur.Role.Name == "Admin"))
            {
                throw new UnauthorizedAccessException("Only admins can view active prompts");
            }

            var activePrompt = await _unitOfWork.GlobalConversationPrompts.GetActiveDefaultPromptAsync();
            if (activePrompt == null)
                throw new InvalidOperationException("No active prompt found");

            return MapToDto(activePrompt);
        }

        private GlobalConversationPromptDto MapToDto(GlobalConversationPrompt prompt)
        {
            return new GlobalConversationPromptDto
            {
                GlobalPromptID = prompt.GlobalPromptID,
                PromptName = prompt.PromptName,
                Description = prompt.Description,
                MasterPromptTemplate = prompt.MasterPromptTemplate,
                ScenarioGuidelines = prompt.ScenarioGuidelines,
                RoleplayInstructions = prompt.RoleplayInstructions,
                EvaluationCriteria = prompt.EvaluationCriteria,
                Status = prompt.Status,
                IsActive = prompt.IsActive,
                IsDefault = prompt.IsDefault,
                UsageCount = prompt.UsageCount,
                CreatedAt = prompt.CreatedAt,
                UpdatedAt = prompt.UpdatedAt
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
            existingPrompt.UpdatedAt = TimeHelper.GetVietnamTime(); ;

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
            existingPrompt.UpdatedAt = TimeHelper.GetVietnamTime(); ;

            await _unitOfWork.GlobalConversationPrompts.UpdateAsync(existingPrompt);
            await _unitOfWork.SaveChangesAsync();

            return true;
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


        public async Task<IEnumerable<AdminProgramDetailDto>> GetProgramsByLanguageAsync(Guid languageId)
        {
            var language = await _unitOfWork.Languages.GetByIdAsync(languageId);
            var languageName = language?.LanguageName ?? "Không rõ";

            var allPrograms = await _unitOfWork.Programs.GetAllAsync();
            var programsInLanguage = allPrograms.Where(p => p.LanguageId == languageId).ToList();
            var allLevels = await _unitOfWork.Levels.GetAllAsync();

            var result = new List<AdminProgramDetailDto>();
            foreach (var program in programsInLanguage)
            {
                var programLevels = allLevels
                    .Where(l => l.ProgramId == program.ProgramId)
                    .OrderBy(l => l.OrderIndex)
                    .Select(MapToLevelDto)
                    .ToList();

                result.Add(MapToProgramDetailDto(program, languageName, programLevels));
            }
            return result;
        }

        public async Task<AdminProgramDetailDto?> GetProgramByIdAsync(Guid programId)
        {
            var program = await _unitOfWork.Programs.GetByIdAsync(programId);
            if (program == null) return null;

            // Lấy tên ngôn ngữ
            var language = await _unitOfWork.Languages.GetByIdAsync(program.LanguageId);
            var languageName = language?.LanguageName ?? "Không rõ";

            // Lấy Levels
            var allLevels = await _unitOfWork.Levels.GetAllAsync();
            var programLevels = allLevels
                .Where(l => l.ProgramId == program.ProgramId)
                .OrderBy(l => l.OrderIndex)
                .Select(MapToLevelDto)
                .ToList();

            return MapToProgramDetailDto(program, languageName, programLevels);
        }

        public async Task<AdminProgramDetailDto> CreateProgramAsync(ProgramCreateDto dto)
        {
            var language = await _unitOfWork.Languages.GetByIdAsync(dto.LanguageId);
            if (language == null)
                throw new ArgumentException("LanguageId không tồn tại.");

            var newProgram = new Program
            {
                ProgramId = Guid.NewGuid(),
                LanguageId = dto.LanguageId,
                Name = dto.Name,
                Description = dto.Description,
                Status = true,
                CreatedAt = TimeHelper.GetVietnamTime(),
                UpdatedAt = TimeHelper.GetVietnamTime()
            };

            await _unitOfWork.Programs.CreateAsync(newProgram);
            await _unitOfWork.SaveChangesAsync();

            // Trả về DTO (với danh sách Level rỗng và tên ngôn ngữ)
            return MapToProgramDetailDto(newProgram, language.LanguageName, new List<AdminProgramLevelDto>());
        }

        public async Task<AdminProgramDetailDto> UpdateProgramAsync(Guid programId, ProgramUpdateDto dto)
        {
            var program = await _unitOfWork.Programs.GetByIdAsync(programId);
            if (program == null)
                throw new KeyNotFoundException("Không tìm thấy chương trình.");

            program.Name = dto.Name;
            program.Description = dto.Description;
            program.Status = dto.Status;
            program.UpdatedAt = TimeHelper.GetVietnamTime(); ;

            _unitOfWork.Programs.Update(program);
            await _unitOfWork.SaveChangesAsync();

            // Lấy thông tin đầy đủ để trả về DTO
            return await GetProgramByIdAsync(programId) ?? throw new Exception("Không thể tải lại Program sau khi cập nhật");
        }

        public async Task SoftDeleteProgramAsync(Guid programId)
        {
            var program = await _unitOfWork.Programs.GetByIdAsync(programId);
            if (program == null)
                throw new KeyNotFoundException("Không tìm thấy chương trình.");

            var allCourses = await _unitOfWork.Courses.GetAllAsync();
            bool hasCourses = allCourses.Any(c => c.ProgramId == programId);

            if (hasCourses)
            {
                _logger.LogWarning("Admin cố gắng xóa Program {ProgramId} nhưng vẫn còn khóa học.", programId);
                throw new InvalidOperationException("Không thể xóa chương trình này. Vẫn còn các khóa học đang liên kết với nó.");
            }

            program.Status = false;
            program.UpdatedAt = TimeHelper.GetVietnamTime(); ;

            _unitOfWork.Programs.Update(program);
            await _unitOfWork.SaveChangesAsync();
        }



        public async Task<IEnumerable<AdminProgramLevelDto>> GetLevelsByProgramAsync(Guid programId)
        {
            var levels = await _unitOfWork.Levels.GetAllAsync();
            return levels.Where(l => l.ProgramId == programId)
                         .OrderBy(l => l.OrderIndex)
                         .Select(MapToLevelDto);
        }

        public async Task<AdminProgramLevelDto?> GetLevelByIdAsync(Guid levelId)
        {
            var level = await _unitOfWork.Levels.GetByIdAsync(levelId);
            return (level == null) ? null : MapToLevelDto(level);
        }

        public async Task<AdminProgramLevelDto> CreateLevelAsync(LevelCreateDto dto)
        {
            var program = await _unitOfWork.Programs.GetByIdAsync(dto.ProgramId);
            if (program == null)
                throw new ArgumentException("ProgramId không tồn tại.");


            var allLevels = await _unitOfWork.Levels.GetAllAsync();
            var levelsInProgram = allLevels.Where(l => l.ProgramId == dto.ProgramId);

            int nextOrderIndex = 1;
            if (levelsInProgram.Any())
            {

                nextOrderIndex = levelsInProgram.Max(l => l.OrderIndex) + 1;
            }


            var newLevel = new Level
            {
                LevelId = Guid.NewGuid(),
                ProgramId = dto.ProgramId,
                Name = dto.Name,
                Description = dto.Description,
                OrderIndex = nextOrderIndex,
                Status = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Levels.CreateAsync(newLevel);
            await _unitOfWork.SaveChangesAsync();

            return MapToLevelDto(newLevel);
        }

        public async Task<AdminProgramLevelDto> UpdateLevelAsync(Guid levelId, LevelUpdateDto dto)
        {
            var level = await _unitOfWork.Levels.GetByIdAsync(levelId);
            if (level == null)
                throw new KeyNotFoundException("Không tìm thấy cấp độ.");

            level.Name = dto.Name;
            level.Description = dto.Description;
            level.OrderIndex = dto.OrderIndex;
            level.Status = dto.Status;
            level.UpdatedAt = TimeHelper.GetVietnamTime(); ;

            _unitOfWork.Levels.Update(level);
            await _unitOfWork.SaveChangesAsync();

            return MapToLevelDto(level); // Dùng hàm Map
        }

        public async Task SoftDeleteLevelAsync(Guid levelId)
        {
            var level = await _unitOfWork.Levels.GetByIdAsync(levelId);
            if (level == null)
                throw new KeyNotFoundException("Không tìm thấy cấp độ.");

            var allCourses = await _unitOfWork.Courses.GetAllAsync();
            bool hasCourses = allCourses.Any(c => c.LevelId == levelId);

            if (hasCourses)
            {
                _logger.LogWarning("Admin cố gắng xóa Level {LevelId} nhưng vẫn còn khóa học.", levelId);
                throw new InvalidOperationException("Không thể xóa cấp độ này. Vẫn còn các khóa học đang liên kết với nó.");
            }

            level.Status = false;
            level.UpdatedAt = TimeHelper.GetVietnamTime(); ;

            _unitOfWork.Levels.Update(level);
            await _unitOfWork.SaveChangesAsync();
        }

        private AdminProgramLevelDto MapToLevelDto(Level level)
        {
            return new AdminProgramLevelDto
            {
                LevelId = level.LevelId,
                ProgramId = level.ProgramId,
                Name = level.Name,
                Description = level.Description ?? "",
                OrderIndex = level.OrderIndex,
                Status = level.Status
            };
        }

        private AdminProgramDetailDto MapToProgramDetailDto(Program program, string languageName, List<AdminProgramLevelDto> levels)
        {
            return new AdminProgramDetailDto
            {
                ProgramId = program.ProgramId,
                LanguageId = program.LanguageId,
                LanguageName = languageName,
                Name = program.Name,
                Description = program.Description ?? "",
                Status = program.Status,
                CreatedAt = program.CreatedAt,
                UpdatedAt = program.UpdatedAt,
                Levels = levels
            };
        }
    }
}





