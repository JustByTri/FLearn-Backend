using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRecordingRepository : IGenericRepository<Recording>
    {
        Task<List<Recording>> GetRecordingsByUserAsync(Guid userId);
        Task<List<Recording>> GetRecordingsByConversationAsync(Guid conversationId);
        Task<List<Recording>> GetRecordingsByLanguageAsync(Guid languageId);
        Task<List<Recording>> GetRecordingsByFormatAsync(string format);
        Task<Recording> GetRecordingByUrlAsync(string url);
    }
}
