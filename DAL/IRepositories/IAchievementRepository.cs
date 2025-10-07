using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IAchievementRepository : IGenericRepository<Achievement>
    {
        Task<List<Achievement>> GetAchievementsByTitleAsync(string title);
        Task<Achievement> GetByTitleAsync(string title);
        Task<List<Achievement>> GetActiveAchievementsAsync();
    }
}
