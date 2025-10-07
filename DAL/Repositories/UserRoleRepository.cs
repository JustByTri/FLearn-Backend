using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class UserRoleRepository : GenericRepository<UserRole>, IUserRoleRepository
    {
        public UserRoleRepository(AppDbContext context) : base(context) { }

        public async Task<List<UserRole>> GetRolesByUserAsync(Guid userId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .Where(ur => ur.UserID == userId)
            .ToListAsync();
        }

        public async Task<List<UserRole>> GetUsersByRoleAsync(Guid roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .Where(ur => ur.RoleID == roleId)
            .ToListAsync();
        }

        public async Task<bool> HasUserRoleAsync(Guid userId, Guid roleId)
        {
            return await _context.UserRoles
                .AnyAsync(ur => ur.UserID == userId && ur.RoleID == roleId);
        }

        public async Task<UserRole> GetUserRoleAsync(Guid userId, Guid roleId)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Include(ur => ur.User)
                .FirstOrDefaultAsync(ur => ur.UserID == userId && ur.RoleID == roleId);
        }

        public async Task<bool> RemoveUserRoleAsync(Guid userId, Guid roleId)
        {
            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserID == userId && ur.RoleID == roleId);

            if (userRole != null)
            {
                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
