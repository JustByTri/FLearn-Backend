using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class PurchaseRepository : GenericRepository<Purchase>, IPurchaseRepository
    {
        public PurchaseRepository(AppDbContext context) : base(context) { }

    }
}
