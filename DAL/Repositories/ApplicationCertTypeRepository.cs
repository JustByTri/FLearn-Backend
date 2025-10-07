using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ApplicationCertTypeRepository : GenericRepository<ApplicationCertType>, IApplicationCertTypeRepository
    {
        public ApplicationCertTypeRepository(AppDbContext context) : base(context) { }
    }
}
