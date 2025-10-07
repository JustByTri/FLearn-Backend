using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LearnerLanguageRepository : GenericRepository<LearnerLanguage>, ILearnerLanguageRepository
    {
        public LearnerLanguageRepository(AppDbContext context) : base(context) { }
    }
}
