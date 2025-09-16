using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRoleRepository : IGenericRepository<Role>
    {
        Task<Role> GetByNameAsync(string name);
        Task<List<Role>> GetActiveRolesAsync();
        Task<bool> IsRoleNameExistsAsync(string name);
        Task<List<User>> GetUsersByRoleAsync(Guid roleId);
    }
}
