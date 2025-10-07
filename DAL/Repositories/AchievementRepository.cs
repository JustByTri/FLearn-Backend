using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class AchievementRepository : GenericRepository<Achievement>, IAchievementRepository
    {
        public AchievementRepository(AppDbContext context) : base(context) { }

        public async Task<List<Achievement>> GetAchievementsByTitleAsync(string title)
        {
            return await _context.Achievements
                .Where(a => a.Title.Contains(title))
            .ToListAsync();
        }

        public async Task<Achievement> GetByTitleAsync(string title)
        {
            return await _context.Achievements
                .FirstOrDefaultAsync(a => a.Title == title)
                ?? throw new Exception("Achievement not found");
        }

        public async Task<List<Achievement>> GetActiveAchievementsAsync()
        {
            return await _context.Achievements
                .OrderBy(a => a.Title)
                .ToListAsync();
        }
    }
}
