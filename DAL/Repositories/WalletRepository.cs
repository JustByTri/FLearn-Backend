using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class WalletRepository : GenericRepository<Wallet>, IWalletRepository
    {
        private readonly AppDbContext _context;
        public WalletRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<Wallet?> GetByTeacherIdAsync(Guid teacherId)
        {
            return await _context.Wallets.FirstOrDefaultAsync(w => w.TeacherId == teacherId);
        }
    }
}
