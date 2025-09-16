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
    public class RoadmapRepository : GenericRepository<Roadmap>, IRoadmapRepository
    {
        public RoadmapRepository(AppDbContext context) : base(context) { }

        public async Task<List<Roadmap>> GetRoadmapsByUserAsync(Guid userId)
        {
            return await _context.Roadmaps
                .Include(r => r.Language)
                .Include(r => r.RoadmapDetails)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Roadmap>> GetActiveRoadmapsByUserAsync(Guid userId)
        {
            return await _context.Roadmaps
                .Include(r => r.Language)
                .Include(r => r.RoadmapDetails)
                .Where(r => r.UserID == userId && r.IsActive)
                .OrderByDescending(r => r.UpdatedAt)
            .ToListAsync();
        }

        public async Task<Roadmap> GetRoadmapWithDetailsAsync(Guid roadmapId)
        {
            return await _context.Roadmaps
                .Include(r => r.Language)
                .Include(r => r.User)
                .Include(r => r.RoadmapDetails.OrderBy(rd => rd.StepNumber))
                .FirstOrDefaultAsync(r => r.RoadmapID == roadmapId);
        }

        public async Task<List<Roadmap>> GetRoadmapsByLanguageAsync(Guid languageId)
        {
            return await _context.Roadmaps
                .Include(r => r.User)
                .Include(r => r.RoadmapDetails)
                .Where(r => r.LanguageID == languageId && r.IsActive)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}
