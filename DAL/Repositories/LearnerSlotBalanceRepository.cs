using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LearnerSlotBalanceRepository : GenericRepository<LearnerSlotBalance>, ILearnerSlotBalanceRepository
    {
        public LearnerSlotBalanceRepository(AppDbContext context) : base(context) { }
    }
}
