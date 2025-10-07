using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class SlotPurchaseRepository : GenericRepository<SlotPurchase>, ISlotPurchaseRepository
    {
        public SlotPurchaseRepository(AppDbContext context) : base(context) { }
    }
}
