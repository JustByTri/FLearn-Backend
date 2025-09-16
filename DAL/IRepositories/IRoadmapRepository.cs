using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRoadmapRepository : IGenericRepository<Roadmap>
    {
        Task<List<Roadmap>> GetRoadmapsByUserAsync(Guid userId);
        Task<List<Roadmap>> GetActiveRoadmapsByUserAsync(Guid userId);
        Task<Roadmap> GetRoadmapWithDetailsAsync(Guid roadmapId);
        Task<List<Roadmap>> GetRoadmapsByLanguageAsync(Guid languageId);
    }
}
