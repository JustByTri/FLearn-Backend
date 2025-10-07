using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;

namespace DAL.Repositories
{
    public class TransactionRepository : GenericRepository<DAL.Models.Transaction>, ITransactionRepository
    {
        public TransactionRepository(AppDbContext context) : base(context) { }
    }
}
