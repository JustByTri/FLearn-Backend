using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IPurchasesDetailRepository : IGenericRepository<PurchasesDetail>
    {
        Task<List<PurchasesDetail>> GetDetailsByPurchaseAsync(Guid purchaseId);
        Task<List<PurchasesDetail>> GetDetailsByCourseAsync(Guid courseId);
        Task<PurchasesDetail> GetDetailByPurchaseAndCourseAsync(Guid purchaseId, Guid courseId);
        Task<bool> HasUserPurchasedCourseAsync(Guid userId, Guid courseId);
    }
}
