using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class RoadmapDetailRepository : GenericRepository<RoadmapDetail>, IRoadmapDetailRepository
    {
        public RoadmapDetailRepository(AppDbContext context) : base(context) { }
    }
}
