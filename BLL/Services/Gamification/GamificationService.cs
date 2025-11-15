using BLL.IServices.Gamification;
using DAL.Helpers;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Gamification
{
    public class GamificationService : IGamificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<GamificationService> _logger;
        // Simple level curve: Level n requires sum_{i=1..n} (i * 100) XP
        // Which makes level thresholds: 0, 100, 300, 600, 1000, ...
        private const int BaseXpPerLevel = 100;

        public GamificationService(IUnitOfWork unitOfWork, ILogger<GamificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(int totalXp, int todayXp, int newLevel)> AwardXpAsync(LearnerLanguage learner, int xp, string reason)
        {
            if (xp <= 0) return (learner.ExperiencePoints, learner.TodayXp, GetLevelFromXp(learner.ExperiencePoints));

            // Daily reset if needed
            await EnsureDailyXpResetAsync(learner);

            learner.ExperiencePoints += xp;
            learner.TodayXp += xp;
            learner.UpdatedAt = TimeHelper.GetVietnamTime();
            await _unitOfWork.LearnerLanguages.UpdateAsync(learner);
            await _unitOfWork.SaveChangesAsync();

            var level = GetLevelFromXp(learner.ExperiencePoints);
            _logger.LogInformation("Awarded {XP} XP to LearnerLanguage {Id} for {Reason}. Total={Total}, Today={Today}, Level={Level}", xp, learner.LearnerLanguageId, reason, learner.ExperiencePoints, learner.TodayXp, level);

            return (learner.ExperiencePoints, learner.TodayXp, level);
        }

        public int GetLevelFromXp(int totalXp)
        {
            // Solve n such that n(n+1)/2 * BaseXpPerLevel <= totalXp
            // n^2 + n - (2*totalXp/BaseXpPerLevel) <= 0
            if (totalXp <= 0) return 0;
            double c = 2.0 * totalXp / BaseXpPerLevel;
            var n = (int)Math.Floor((-1 + Math.Sqrt(1 + 4 * c)) / 2.0);
            return Math.Max(0, n);
        }

        public double GetLevelProgress(int totalXp)
        {
            var level = GetLevelFromXp(totalXp);
            var prevThreshold = ThresholdXpForLevel(level);
            var nextThreshold = ThresholdXpForLevel(level + 1);
            if (nextThreshold == prevThreshold) return 1.0;
            return Math.Clamp((totalXp - prevThreshold) / (double)(nextThreshold - prevThreshold), 0.0, 1.0);
        }

        public async Task EnsureDailyXpResetAsync(LearnerLanguage learner)
        {
            var now = TimeHelper.GetVietnamTime();
            if (learner.LastXpResetDate.Date != now.Date)
            {
                // Update streak if met daily goal yesterday
                if (learner.TodayXp >= learner.DailyXpGoal)
                {
                    learner.StreakDays += 1;
                }
                else
                {
                    // break streak
                    learner.StreakDays = 0;
                }
                learner.TodayXp = 0;
                learner.LastXpResetDate = now;
                learner.UpdatedAt = now;
                await _unitOfWork.LearnerLanguages.UpdateAsync(learner);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        private static int ThresholdXpForLevel(int level)
        {
            if (level <= 0) return 0;
            // sum of 1..level = level*(level+1)/2
            return (level * (level + 1) / 2) * BaseXpPerLevel;
        }
    }
}
