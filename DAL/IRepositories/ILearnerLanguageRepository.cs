using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ILearnerLanguageRepository : IGenericRepository<LearnerLanguage>
    {
        Task<IEnumerable<LearnerLanguage>> GetLeaderboardAsync(Guid languageId, int count);
        Task<int> GetRankAsync(Guid languageId, int streakDays);
    }
}
