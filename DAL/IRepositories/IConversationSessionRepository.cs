using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IConversationSessionRepository : IGenericRepository<ConversationSession>
    {
        Task<List<ConversationSession>> GetUserSessionsAsync(Guid userId);
        Task<ConversationSession?> GetSessionWithMessagesAsync(Guid sessionId);
        Task<List<ConversationSession>> GetCompletedSessionsAsync(Guid userId);
        Task<List<ConversationSession>> GetActiveSessionsAsync(Guid userId);
    }
}
