using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class PurchaseDetailRepository : GenericRepository<PurchaseDetail>, IPurchaseDetailRepository
    {
        public PurchaseDetailRepository(AppDbContext context) : base(context) { }

    }
}
