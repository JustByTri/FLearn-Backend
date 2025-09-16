using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IUserAchievementRepository : IGenericRepository<UserAchievement>
    {
        Task<List<UserAchievement>> GetAchievementsByUserAsync(Guid userId);
        Task<List<UserAchievement>> GetUsersByAchievementAsync(Guid achievementId);
        Task<bool> HasUserAchievementAsync(Guid userId, Guid achievementId);
        Task<UserAchievement> GetUserAchievementAsync(Guid userId, Guid achievementId);
        Task<List<UserAchievement>> GetRecentAchievementsByUserAsync(Guid userId, int count);
    }
}
