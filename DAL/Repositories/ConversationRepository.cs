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
    public class ConversationRepository : GenericRepository<Conversation>, IConversationRepository
    {
        public ConversationRepository(AppDbContext context) : base(context) { }

        public async Task<List<Conversation>> GetByUserIdAsync(Guid userId)
        {
            return await _context.Conversations
                .Include(c => c.Language)
                .Include(c => c.User)
                .Where(c => c.UserID == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetByLanguageIdAsync(Guid languageId)
        {
            return await _context.Conversations
                .Include(c => c.Language)
                .Include(c => c.User)
                .Where(c => c.LanguageID == languageId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Conversation>> GetActiveConversationsAsync(Guid userId)
        {
            return await _context.Conversations
                .Include(c => c.Language)
                .Include(c => c.User)
                .Where(c => c.UserID == userId && c.EndedAt == null)
                .OrderByDescending(c => c.StartedAt)
                .ToListAsync();
        }

        public async Task<Conversation> GetConversationWithFeedbackAsync(Guid conversationId)
        {
            return await _context.Conversations
                .Include(c => c.Language)
                .Include(c => c.User)
                .Include(c => c.AIFeedBacks)
                .Include(c => c.Recordings)
                .FirstOrDefaultAsync(c => c.ConversationID == conversationId);
        }

        public async Task<List<Conversation>> GetByTopicAsync(string topic)
        {
            return await _context.Conversations
                .Include(c => c.Language)
                .Include(c => c.User)
                .Where(c => c.Topic.Contains(topic))
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}
