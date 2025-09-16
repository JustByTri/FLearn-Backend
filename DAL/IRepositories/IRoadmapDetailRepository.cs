using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRoadmapDetailRepository : IGenericRepository<RoadmapDetail>
    {
        Task<List<RoadmapDetail>> GetDetailsByRoadmapAsync(Guid roadmapId);
        Task<RoadmapDetail> GetDetailByStepAsync(Guid roadmapId, int stepNumber);
        Task<List<RoadmapDetail>> GetCompletedDetailsAsync(Guid roadmapId);
        Task<RoadmapDetail> GetNextIncompleteStepAsync(Guid roadmapId);
    }
}
