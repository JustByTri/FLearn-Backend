using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LearnerProgressRepository : GenericRepository<LearnerProgress>, ILearnerProgressRepository
    {
        public LearnerProgressRepository(AppDbContext context) : base(context) { }
    }
}
