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
    public class ConversationMessageRepository : GenericRepository<ConversationMessage>, IConversationMessageRepository
    {
        public ConversationMessageRepository(AppDbContext context) : base(context) { }

        public async Task<List<ConversationMessage>> GetSessionMessagesAsync(Guid sessionId)
        {
            return await _context.ConversationMessages
                .Where(m => m.ConversationSessionID == sessionId)
                .OrderBy(m => m.SequenceOrder)
                .ToListAsync();
        }

        public async Task<ConversationMessage?> GetLastMessageAsync(Guid sessionId)
        {
            return await _context.ConversationMessages
                .Where(m => m.ConversationSessionID == sessionId)
                .OrderByDescending(m => m.SequenceOrder)
            .FirstOrDefaultAsync();
        }

        public async Task<int> GetMessageCountAsync(Guid sessionId)
        {
            return await _context.ConversationMessages
                .CountAsync(m => m.ConversationSessionID == sessionId);
        }
    }
}
