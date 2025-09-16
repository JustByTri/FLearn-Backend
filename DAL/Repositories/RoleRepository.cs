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
    public class RoleRepository : GenericRepository<Role>, IRoleRepository
    {
        public RoleRepository(AppDbContext context) : base(context) { }

        public async Task<Role> GetByNameAsync(string name)
        {
            return await _context.Roles
                .Include(r => r.UserRoles)
                .FirstOrDefaultAsync(r => r.Name == name);
        }

        public async Task<List<Role>> GetActiveRolesAsync()
        {
            return await _context.Roles
                .Include(r => r.UserRoles)
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        public async Task<bool> IsRoleNameExistsAsync(string name)
        {
            return await _context.Roles.AnyAsync(r => r.Name == name);
        }

        public async Task<List<User>> GetUsersByRoleAsync(Guid roleId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.RoleID == roleId))
                .ToListAsync();
        }
    }
}
