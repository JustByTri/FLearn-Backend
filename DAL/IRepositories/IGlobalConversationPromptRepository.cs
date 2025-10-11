using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IGlobalConversationPromptRepository : IGenericRepository<GlobalConversationPrompt>
    {
        Task<GlobalConversationPrompt?> GetActiveDefaultPromptAsync();
        Task<List<GlobalConversationPrompt>> GetActivePromptsAsync();
        Task<bool> SetAsDefaultAsync(Guid promptId);
    }
}
