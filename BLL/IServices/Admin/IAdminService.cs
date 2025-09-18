using Common.DTO.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Admin
{
 public  interface IAdminService
    {
        Task<List<UserListDto>> GetAllUsersAsync(Guid adminUserId);
        Task<List<UserListDto>> GetAllStaffAsync(Guid adminUserId);
        Task<AdminDashboardDto> GetAdminDashboardAsync(Guid adminUserId);
    }
}
