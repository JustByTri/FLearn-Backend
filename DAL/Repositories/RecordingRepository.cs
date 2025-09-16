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
    public class RecordingRepository : GenericRepository<Recording>, IRecordingRepository
    {
        public RecordingRepository(AppDbContext context) : base(context) { }

        public async Task<List<Recording>> GetRecordingsByUserAsync(Guid userId)
        {
            return await _context.Recordings
                .Include(r => r.Language)
                .Include(r => r.Conversation)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Recording>> GetRecordingsByConversationAsync(Guid conversationId)
        {
            return await _context.Recordings
                .Include(r => r.Language)
                .Include(r => r.Conversation)
                .Where(r => r.ConverationID == conversationId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Recording>> GetRecordingsByLanguageAsync(Guid languageId)
        {
            return await _context.Recordings
                .Include(r => r.Language)
                .Include(r => r.Conversation)
                .Where(r => r.LanguageID == languageId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Recording>> GetRecordingsByFormatAsync(string format)
        {
            return await _context.Recordings
                .Include(r => r.Language)
                .Include(r => r.Conversation)
                .Where(r => r.Format == format)
                .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
        }

        public async Task<Recording> GetRecordingByUrlAsync(string url)
        {
            return await _context.Recordings
                .Include(r => r.Language)
                .Include(r => r.Conversation)
                .FirstOrDefaultAsync(r => r.Url == url);
        }
    }
}
