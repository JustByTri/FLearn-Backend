using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IConversationRepository : IGenericRepository<Conversation>
    {
        Task<List<Conversation>> GetByUserIdAsync(Guid userId);
        Task<List<Conversation>> GetByLanguageIdAsync(Guid languageId);
        Task<List<Conversation>> GetActiveConversationsAsync(Guid userId);
        Task<Conversation> GetConversationWithFeedbackAsync(Guid conversationId);
        Task<List<Conversation>> GetByTopicAsync(string topic);
    }
}
