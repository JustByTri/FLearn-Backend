using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IConversationMessageRepository : IGenericRepository<ConversationMessage>
    {
        Task<List<ConversationMessage>> GetSessionMessagesAsync(Guid sessionId);
        Task<ConversationMessage?> GetLastMessageAsync(Guid sessionId);
        Task<int> GetMessageCountAsync(Guid sessionId);
    }
}
