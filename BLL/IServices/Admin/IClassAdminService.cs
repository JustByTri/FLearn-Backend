using Common.DTO.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Admin
{
    public interface IClassAdminService
    {
        Task<List<object>> GetAllDisputesAsync();
        Task<bool> ResolveDisputeAsync(Guid disputeId, ResolveDisputeDto dto);
        Task<bool> TriggerPayoutAsync(Guid classId);

        // ============================================
        // CLASS CANCELLATION MANAGEMENT
        // ============================================

        /// <summary>
        /// Manager duyệt yêu cầu hủy lớp
        /// </summary>
        Task<bool> ApproveCancellationRequestAsync(Guid managerId, Guid requestId, string? note);

        /// <summary>
        /// Manager từ chối yêu cầu hủy lớp
        /// </summary>
        Task<bool> RejectCancellationRequestAsync(Guid managerId, Guid requestId, string reason);

        /// <summary>
        /// Lấy danh sách yêu cầu hủy lớp đang chờ duyệt (theo ngôn ngữ Manager quản lý)
        /// </summary>
        Task<IEnumerable<ClassCancellationRequestDto>> GetPendingCancellationRequestsAsync(Guid managerId);

        /// <summary>
        /// Lấy chi tiết một yêu cầu hủy lớp
        /// </summary>
        Task<ClassCancellationRequestDto> GetCancellationRequestByIdAsync(Guid managerId, Guid requestId);
    }
}
