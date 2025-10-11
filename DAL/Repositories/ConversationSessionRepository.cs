using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using DAL.Type;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ConversationSessionRepository : GenericRepository<ConversationSession>, IConversationSessionRepository
    {
        public ConversationSessionRepository(AppDbContext context) : base(context) { }

        public async Task<List<ConversationSession>> GetUserSessionsAsync(Guid userId)
        {
            return await _context.ConversationSession
                .Include(s => s.Language)
                .Include(s => s.Topic)
                .Include(s => s.GlobalPrompt)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.StartedAt)
            .ToListAsync();
        }

        public async Task<ConversationSession?> GetSessionWithMessagesAsync(Guid sessionId)
        {
            return await _context.ConversationSession
                .Include(s => s.Language)
                .Include(s => s.Topic)
                .Include(s => s.GlobalPrompt)
                .Include(s => s.ConversationMessages.OrderBy(m => m.SequenceOrder))
                .FirstOrDefaultAsync(s => s.ConversationSessionID == sessionId);
        }

        public async Task<List<ConversationSession>> GetCompletedSessionsAsync(Guid userId)
        {
            return await _context.ConversationSession
                .Include(s => s.Language)
                .Include(s => s.Topic)
                .Where(s => s.UserId == userId && s.Status == ConversationSessionStatus.Completed)
                .OrderByDescending(s => s.EndedAt)
            .ToListAsync();
        }

        public async Task<List<ConversationSession>> GetActiveSessionsAsync(Guid userId)
        {
            return await _context.ConversationSession
                .Include(s => s.Language)
                .Include(s => s.Topic)
                .Where(s => s.UserId == userId && s.Status == ConversationSessionStatus.Active)
                .OrderByDescending(s => s.StartedAt)
                .ToListAsync();
        }
    }
}
