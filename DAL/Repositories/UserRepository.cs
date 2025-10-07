using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.UserName == username);
        }

        public async Task<List<User>> GetUsersByRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.Name == roleName))
                .ToListAsync();
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _context.Users.AnyAsync(u => u.Email == email);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _context.Users.AnyAsync(u => u.UserName == username);
        }

        public async Task<User> GetUserWithRolesAsync(Guid userId)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Include(u => u.RefreshTokens)
                .FirstOrDefaultAsync(u => u.UserID == userId);
        }

        public async Task<List<User>> GetActiveUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.Status == true)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }

        public async Task<List<User>> GetUsersByIndustryAsync(string industry)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .Where(u => u.Status == true)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }
        public async Task<List<User>> GetAllUsersWithRolesAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            return await _context.Users.CountAsync(u => u.Status == true);
        }

        public async Task<int> GetUsersCountByRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .CountAsync(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
        }

        public async Task<List<User>> GetRecentUsersAsync(int count)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}

