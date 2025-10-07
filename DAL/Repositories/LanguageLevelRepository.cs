using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LanguageLevelRepository : GenericRepository<LanguageLevel>, ILanguageLevelRepository
    {
        public LanguageLevelRepository(AppDbContext context) : base(context) { }
    }
}
