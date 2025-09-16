using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class PurchasesRepository : GenericRepository<Purchases>, IPurchasesRepository
    {
        public PurchasesRepository(AppDbContext context) : base(context) { }

        public async Task<List<Purchases>> GetPurchasesByUserAsync(Guid userId)
        {
            return await _context.Purchases
                .Include(p => p.PurchasesDetails)
                .Where(p => p.UserID == userId)
                .OrderByDescending(p => p.PurchasedAt)
                .ToListAsync();
        }

        public async Task<Purchases> GetPurchaseWithDetailsAsync(Guid purchaseId)
        {
            return await _context.Purchases
                .Include(p => p.PurchasesDetails)
                    .ThenInclude(pd => pd.Course)
                .FirstOrDefaultAsync(p => p.PurchasesID == purchaseId);
        }

        public async Task<List<Purchases>> GetPurchasesByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            return await _context.Purchases
                .Include(p => p.PurchasesDetails)
                .Where(p => p.PurchasedAt >= startDate && p.PurchasedAt <= endDate)
                .OrderByDescending(p => p.PurchasedAt)
                .ToListAsync();
        }

        public async Task<List<Purchases>> GetPurchasesByPaymentMethodAsync(string paymentMethod)
        {
            return await _context.Purchases
                .Include(p => p.PurchasesDetails)
                .Where(p => p.PaymentMethod == paymentMethod)
                .OrderByDescending(p => p.PurchasedAt)
                .ToListAsync();
        }

        public async Task<decimal> GetTotalPurchaseAmountByUserAsync(Guid userId)
        {
            return await _context.Purchases
                .Where(p => p.UserID == userId)
                .SumAsync(p => p.Amount);
        }
    }
}
