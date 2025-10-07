using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class CertificateTypeRepository : GenericRepository<CertificateType>, ICertificateTypeRepository
    {
        public CertificateTypeRepository(AppDbContext context) : base(context) { }
    }
}
