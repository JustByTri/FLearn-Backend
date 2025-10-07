using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class RoadmapRepository : GenericRepository<Roadmap>, IRoadmapRepository
    {
        public RoadmapRepository(AppDbContext context) : base(context) { }
    }
}
