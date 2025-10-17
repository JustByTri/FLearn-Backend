using Common.DTO.Admin;
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

    }
}
