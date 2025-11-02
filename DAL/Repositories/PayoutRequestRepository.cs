using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class PayoutRequestRepository : GenericRepository<PayoutRequest>, IPayoutRequestRepository
    {
        public PayoutRequestRepository(AppDbContext context) : base(context) { }
    }
}
