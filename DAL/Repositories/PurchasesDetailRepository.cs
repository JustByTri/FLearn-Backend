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
    public class PurchasesDetailRepository : GenericRepository<PurchasesDetail>, IPurchasesDetailRepository
    {
        public PurchasesDetailRepository(AppDbContext context) : base(context) { }

        public async Task<List<PurchasesDetail>> GetDetailsByPurchaseAsync(Guid purchaseId)
        {
            return await _context.PurchasesDetails
                .Include(pd => pd.Course)
                .Include(pd => pd.Purchases)
                .Where(pd => pd.PurchasesID == purchaseId)
                .ToListAsync();
        }

        public async Task<List<PurchasesDetail>> GetDetailsByCourseAsync(Guid courseId)
        {
            return await _context.PurchasesDetails
                .Include(pd => pd.Course)
                .Include(pd => pd.Purchases)
                .Where(pd => pd.CourseID == courseId)
                .ToListAsync();
        }

        public async Task<PurchasesDetail> GetDetailByPurchaseAndCourseAsync(Guid purchaseId, Guid courseId)
        {
            return await _context.PurchasesDetails
                .Include(pd => pd.Course)
                .Include(pd => pd.Purchases)
                .FirstOrDefaultAsync(pd => pd.PurchasesID == purchaseId && pd.CourseID == courseId);
        }

        public async Task<bool> HasUserPurchasedCourseAsync(Guid userId, Guid courseId)
        {
            return await _context.PurchasesDetails
                .Include(pd => pd.Purchases)
                .AnyAsync(pd => pd.Purchases.UserID == userId && pd.CourseID == courseId);
        }
    }
}
