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
    public class ConversationTaskRepository : GenericRepository<ConversationTask>, IConversationTaskRepository
    {
        public ConversationTaskRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ConversationTask>> GetTasksBySessionIdAsync(Guid sessionId)
        {
            return await _context.ConversationTasks
                .Where(t => t.ConversationSessionID == sessionId)
                .OrderBy(t => t.TaskSequence)
                .ToListAsync();
        }
    }
}
