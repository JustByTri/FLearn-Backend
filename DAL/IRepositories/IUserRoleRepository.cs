using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IUserRoleRepository : IGenericRepository<UserRole>
    {
        Task<List<UserRole>> GetRolesByUserAsync(Guid userId);
        Task<List<UserRole>> GetUsersByRoleAsync(Guid roleId);
        Task<bool> HasUserRoleAsync(Guid userId, Guid roleId);
        Task<UserRole> GetUserRoleAsync(Guid userId, Guid roleId);
        Task<bool> RemoveUserRoleAsync(Guid userId, Guid roleId);
    }
}
