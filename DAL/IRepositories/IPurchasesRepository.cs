using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IPurchasesRepository : IGenericRepository<Purchases>
    {
        Task<List<Purchases>> GetPurchasesByUserAsync(Guid userId);
        Task<Purchases> GetPurchaseWithDetailsAsync(Guid purchaseId);
        Task<List<Purchases>> GetPurchasesByDateRangeAsync(DateTime startDate, DateTime endDate);
        Task<List<Purchases>> GetPurchasesByPaymentMethodAsync(string paymentMethod);
        Task<decimal> GetTotalPurchaseAmountByUserAsync(Guid userId);
    }
}
