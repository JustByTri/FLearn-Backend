using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IAIFeedBackRepository : IGenericRepository<AIFeedBack>
    {
        Task<List<AIFeedBack>> GetByConversationIdAsync(Guid conversationId);
        Task<List<AIFeedBack>> GetByUserIdAsync(Guid userId);
        Task<AIFeedBack> GetLatestFeedbackByConversationAsync(Guid conversationId);
        Task<List<AIFeedBack>> GetFeedbacksByScoreRangeAsync(int minScore, int maxScore);
    }
}
