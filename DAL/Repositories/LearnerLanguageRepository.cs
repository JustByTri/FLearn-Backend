using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class LearnerLanguageRepository : GenericRepository<LearnerLanguage>, ILearnerLanguageRepository
    {
        public LearnerLanguageRepository(AppDbContext context) : base(context) { }
        public async Task<IEnumerable<LearnerLanguage>> GetLeaderboardAsync(Guid languageId, int count)
        {
            return await _context.LearnerLanguages
                .AsNoTracking()
                .Include(ll => ll.User)
                .Where(ll => ll.LanguageId == languageId && ll.User.Status == true)
                .OrderByDescending(ll => ll.StreakDays) 
                .Take(count)
                .ToListAsync();
        }

        public async Task<int> GetRankAsync(Guid languageId, int streakDays)
        {
           
            var rank = await _context.LearnerLanguages
                .Include(ll => ll.User)
                .CountAsync(ll => ll.LanguageId == languageId &&
                                 ll.StreakDays > streakDays &&
                                 ll.User.Status == true);

            return rank + 1;
        }

    }
}
