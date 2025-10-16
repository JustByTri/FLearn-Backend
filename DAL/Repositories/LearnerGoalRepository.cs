using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LearnerGoalRepository : GenericRepository<LearnerGoal>, ILearnerGoalRepository
    {
        public LearnerGoalRepository(AppDbContext context) : base(context) { }
    }
}
