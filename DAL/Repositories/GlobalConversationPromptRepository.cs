using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class GlobalConversationPromptRepository : GenericRepository<GlobalConversationPrompt>, IGlobalConversationPromptRepository
    {
        public GlobalConversationPromptRepository(AppDbContext context) : base(context) { }

        public async Task<GlobalConversationPrompt?> GetActiveDefaultPromptAsync()
        {
            return await _context.GlobalConversationPrompts
                .Where(p => p.IsActive && p.IsDefault)
                .FirstOrDefaultAsync();
        }

        public async Task<List<GlobalConversationPrompt>> GetActivePromptsAsync()
        {
            return await _context.GlobalConversationPrompts
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.IsDefault)
                .ThenBy(p => p.PromptName)
            .ToListAsync();
        }

        public async Task<bool> SetAsDefaultAsync(Guid promptId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Remove default from all prompts
                var allPrompts = await _context.GlobalConversationPrompts.ToListAsync();
                foreach (var p in allPrompts)
                {
                    p.IsDefault = false;
                }

                // Set new default
                if (promptId != Guid.Empty)
                {
                    var newDefault = await _context.GlobalConversationPrompts.FindAsync(promptId);
                    if (newDefault != null)
                    {
                        newDefault.IsDefault = true;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                return true;
            }
            catch
            {
                await transaction.RollbackAsync();
                return false;
            }
        }
    }
}
