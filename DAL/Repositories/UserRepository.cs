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
    public class UserRepository : GenericRepository<User>, IUserRepository
    {
        public UserRepository(AppDbContext context) : base(context) { }

        public async Task<User> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.Roadmaps)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.RefreshTokens)
                .Include(u => u.Roadmaps)
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
                .Include(u => u.Roadmaps)
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
                .Where(u => u.Industry == industry && u.Status == true)
                .OrderBy(u => u.UserName)
                .ToListAsync();
        }
    }
}
