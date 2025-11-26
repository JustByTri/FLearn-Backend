using Common.DTO.Admin;
using Common.DTO.Payment;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Admin
{
    public interface IAdminService
    {
        Task<List<UserListDto>> GetAllUsersAsync(Guid adminUserId);
        Task<List<UserListDto>> GetAllStaffAsync(Guid adminUserId);
        Task<AdminDashboardDto> GetAdminDashboardAsync(Guid adminUserId);

        // Global Conversation Prompt Management
        Task<List<GlobalConversationPromptDto>> GetAllGlobalPromptsAsync(Guid adminUserId);
        Task<GlobalConversationPromptDto> GetGlobalPromptByIdAsync(Guid adminUserId, Guid promptId);
        Task<GlobalConversationPromptDto> CreateGlobalPromptAsync(Guid adminUserId, CreateGlobalPromptDto createPromptDto);
        Task<GlobalConversationPromptDto> UpdateGlobalPromptAsync(Guid adminUserId, Guid promptId, UpdateGlobalPromptDto updatePromptDto);
        Task<bool> DeleteGlobalPromptAsync(Guid adminUserId, Guid promptId);
        Task<bool> ToggleGlobalPromptStatusAsync(Guid adminUserId, Guid promptId);
        Task<GlobalConversationPromptDto> GetActiveGlobalPromptAsync(Guid adminUserId);
        Task<GlobalConversationPromptDto> ActivateGlobalPromptAsync(
   Guid adminUserId,
   Guid promptId);


        Task<IEnumerable<AdminProgramDetailDto>> GetProgramsByLanguageAsync(Guid languageId);
        Task<AdminProgramDetailDto?> GetProgramByIdAsync(Guid programId);
        Task<AdminProgramDetailDto> CreateProgramAsync(ProgramCreateDto dto);
        Task<AdminProgramDetailDto> UpdateProgramAsync(Guid programId, ProgramUpdateDto dto);
        Task SoftDeleteProgramAsync(Guid programId);


        Task<IEnumerable<AdminProgramLevelDto>> GetLevelsByProgramAsync(Guid programId);
        Task<AdminProgramLevelDto?> GetLevelByIdAsync(Guid levelId);
        Task<AdminProgramLevelDto> CreateLevelAsync(LevelCreateDto dto);
        Task<AdminProgramLevelDto> UpdateLevelAsync(Guid levelId, LevelUpdateDto dto);
        Task SoftDeleteLevelAsync(Guid levelId);
        Task<WalletDto> GetAdminWalletAsync(Guid adminId);
        Task<bool> BanUserAsync(Guid adminUserId, Guid targetUserId, string reason);


    }
}
