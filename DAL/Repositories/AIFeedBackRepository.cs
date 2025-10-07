using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AIFeedBackRepository : GenericRepository<AIFeedBack>, IAIFeedBackRepository
    {
        public AIFeedBackRepository(AppDbContext context) : base(context) { }

        public async Task<List<AIFeedBack>> GetByConversationIdAsync(Guid conversationId)
        {
            return await _context.AIFeedBacks
                .Include(f => f.Conversation)
                .Where(f => f.ConversationID == conversationId)
                .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();
        }

        public async Task<List<AIFeedBack>> GetByUserIdAsync(Guid userId)
        {
            return await _context.AIFeedBacks
                .Include(f => f.Conversation)
                .Where(f => f.Conversation.UserId == userId)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }

        public async Task<AIFeedBack> GetLatestFeedbackByConversationAsync(Guid conversationId)
        {
            return await _context.AIFeedBacks
                .Include(f => f.Conversation)
                .Where(f => f.ConversationID == conversationId)
                .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<AIFeedBack>> GetFeedbacksByScoreRangeAsync(int minScore, int maxScore)
        {
            return await _context.AIFeedBacks
                .Include(f => f.Conversation)
                .OrderByDescending(f => f.CreatedAt)
                .ToListAsync();
        }
    }
}
