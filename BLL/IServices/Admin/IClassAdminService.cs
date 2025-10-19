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
    }
}
