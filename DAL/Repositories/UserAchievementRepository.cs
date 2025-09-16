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
    public class UserAchievementRepository : GenericRepository<UserAchievement>, IUserAchievementRepository
    {
        public UserAchievementRepository(AppDbContext context) : base(context) { }

        public async Task<List<UserAchievement>> GetAchievementsByUserAsync(Guid userId)
        {
            return await _context.UserAchievements
                .Where(ua => ua.UserID == userId)
                .OrderByDescending(ua => ua.AchievedAt)
                .ToListAsync();
        }

        public async Task<List<UserAchievement>> GetUsersByAchievementAsync(Guid achievementId)
        {
            return await _context.UserAchievements
                .Where(ua => ua.AchievementID == achievementId)
                .OrderByDescending(ua => ua.AchievedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserAchievementAsync(Guid userId, Guid achievementId)
        {
            return await _context.UserAchievements
                .AnyAsync(ua => ua.UserID == userId && ua.AchievementID == achievementId);
        }

        public async Task<UserAchievement> GetUserAchievementAsync(Guid userId, Guid achievementId)
        {
            return await _context.UserAchievements
                .FirstOrDefaultAsync(ua => ua.UserID == userId && ua.AchievementID == achievementId);
        }

        public async Task<List<UserAchievement>> GetRecentAchievementsByUserAsync(Guid userId, int count)
        {
            return await _context.UserAchievements
                .Where(ua => ua.UserID == userId)
                .OrderByDescending(ua => ua.AchievedAt)
                .Take(count)
                .ToListAsync();
        }
    }
}
