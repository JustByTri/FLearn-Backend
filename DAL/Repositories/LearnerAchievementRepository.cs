using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LearnerAchievementRepository : GenericRepository<LearnerAchievement>, ILearnerAchievementRepository
    {
        public LearnerAchievementRepository(AppDbContext context) : base(context) { }
    }
}
