using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRefundRequestsRepository : IGenericRepository<RefundRequest>
    {
       
        Task<RefundRequest> GetByIdAsync(Guid refundRequestId);
        Task<RefundRequest> GetByIdWithDetailsAsync(Guid refundRequestId);
        Task<RefundRequest> GetPendingRefundByEnrollmentAsync(Guid enrollmentId);
        Task<List<RefundRequest>> GetRefundRequestsByStudentAsync(Guid studentId);
        Task<List<RefundRequest>> GetRefundRequestsByStatusAsync(RefundRequestStatus status);
        Task<List<RefundRequest>> GetAllPendingRefundRequestsAsync();
        Task<IEnumerable<RefundRequest>> GetByLearnerIdAsync(Guid learnerId);
        Task<int> GetPendingCountAsync();
    }
}
