using System;using System.Collections.Generic;using System.Linq;using System.Threading.Tasks;
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
        private const int BaseXpPerLevel = 100;

        public GamificationService(IUnitOfWork unitOfWork, ILogger<GamificationService> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public async Task<(int totalXp, int todayXp, int newLevel)> AwardXpAsync(LearnerLanguage learner, int xp, string reason)
        {
            if (xp <= 0) return (learner.ExperiencePoints, learner.TodayXp, GetLevelFromXp(learner.ExperiencePoints));
            await EnsureDailyXpResetAsync(learner);
            learner.ExperiencePoints += xp;
            learner.TodayXp += xp;
            learner.UpdatedAt = TimeHelper.GetVietnamTime();
            await _unitOfWork.LearnerLanguages.UpdateAsync(learner);
            try { await LogXpAsync(learner.LearnerLanguageId, xp, reason); }
            catch (Exception ex) { _logger.LogWarning(ex, "Failed to log XP event. Continuing without log."); }
            await _unitOfWork.SaveChangesAsync();
            var level = GetLevelFromXp(learner.ExperiencePoints);
            _logger.LogInformation("Awarded {XP} XP to LearnerLanguage {Id} for {Reason}. Total={Total}, Today={Today}, Level={Level}", xp, learner.LearnerLanguageId, reason, learner.ExperiencePoints, learner.TodayXp, level);
            return (learner.ExperiencePoints, learner.TodayXp, level);
        }

        public int GetLevelFromXp(int totalXp)
        {
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
                if (learner.TodayXp >= learner.DailyXpGoal)
                {
                    learner.StreakDays += 1;
                }
                else
                {
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
            return (level * (level + 1) / 2) * BaseXpPerLevel;
        }

        public async Task LogXpAsync(Guid learnerLanguageId, int amount, string reason)
        {
            var evt = new LearnerXpEvent
            {
                LearnerXpEventId = Guid.NewGuid(),
                LearnerLanguageId = learnerLanguageId,
                Amount = amount,
                Reason = reason,
                CreatedAt = TimeHelper.GetVietnamTime()
            };
            await _unitOfWork.LearnerXpEvents.CreateAsync(evt);
        }

        public async Task<Dictionary<DateTime, int>> GetDailyXpAsync(Guid learnerLanguageId, DateTime from, DateTime to)
        {
            try
            {
                var events = await _unitOfWork.LearnerXpEvents.FindAllAsync(e => e.LearnerLanguageId == learnerLanguageId && e.CreatedAt >= from && e.CreatedAt <= to);
                return events
                    .GroupBy(e => e.CreatedAt.Date)
                    .ToDictionary(g => g.Key, g => g.Sum(x => x.Amount));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "GetDailyXpAsync failed, returning empty aggregation.");
                return new Dictionary<DateTime, int>();
            }
        }
    }
}
