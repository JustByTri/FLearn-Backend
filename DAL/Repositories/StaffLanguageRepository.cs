using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class StaffLanguageRepository : GenericRepository<StaffLanguage>, IStaffLanguageRepository
    {
        public StaffLanguageRepository(AppDbContext context) : base(context) { }
    }
}
