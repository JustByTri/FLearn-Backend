using System.Threading.Tasks;
using DAL.Models;

namespace BLL.IServices.Gamification
{
    public interface IGamificationService
    {
        // Award XP to a learner language; returns new totals
        Task<(int totalXp, int todayXp, int newLevel)> AwardXpAsync(LearnerLanguage learner, int xp, string reason);
        // Calculate current level from total XP
        int GetLevelFromXp(int totalXp);
        // Get progress within current level (0..1)
        double GetLevelProgress(int totalXp);
        // Ensure daily reset (today XP and streak) for a learner
        Task EnsureDailyXpResetAsync(LearnerLanguage learner);
    }
}
