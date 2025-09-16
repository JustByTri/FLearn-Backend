using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
