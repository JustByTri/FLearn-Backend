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
    public class RoadmapDetailRepository : GenericRepository<RoadmapDetail>, IRoadmapDetailRepository
    {
        public RoadmapDetailRepository(AppDbContext context) : base(context) { }

        public async Task<List<RoadmapDetail>> GetDetailsByRoadmapAsync(Guid roadmapId)
        {
            return await _context.RoadmapDetails
                .Include(rd => rd.Roadmap)
                .Where(rd => rd.RoadmapID == roadmapId)
                .OrderBy(rd => rd.StepNumber)
                .ToListAsync();
        }

        public async Task<RoadmapDetail> GetDetailByStepAsync(Guid roadmapId, int stepNumber)
        {
            return await _context.RoadmapDetails
                .Include(rd => rd.Roadmap)
                .FirstOrDefaultAsync(rd => rd.RoadmapID == roadmapId && rd.StepNumber == stepNumber);
        }

        public async Task<List<RoadmapDetail>> GetCompletedDetailsAsync(Guid roadmapId)
        {
            return await _context.RoadmapDetails
                .Include(rd => rd.Roadmap)
                .Where(rd => rd.RoadmapID == roadmapId && rd.IsCompleted)
                .OrderBy(rd => rd.StepNumber)
                .ToListAsync();
        }

        public async Task<RoadmapDetail> GetNextIncompleteStepAsync(Guid roadmapId)
        {
            return await _context.RoadmapDetails
                .Include(rd => rd.Roadmap)
                .Where(rd => rd.RoadmapID == roadmapId && !rd.IsCompleted)
                .OrderBy(rd => rd.StepNumber)
                .FirstOrDefaultAsync();
        }
    }
}
