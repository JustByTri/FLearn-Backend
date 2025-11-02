using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ManagerLanguageRepository : GenericRepository<ManagerLanguage>, IManagerLanguageRepository
    {
        public ManagerLanguageRepository(AppDbContext context) : base(context) { }
    }
}
