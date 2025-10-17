using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IConversationTaskRepository : IGenericRepository<ConversationTask>
    {
        Task<List<ConversationTask>> GetTasksBySessionIdAsync(Guid sessionId);
    }
}
