using System;

namespace Common.DTO.Gamification
{
    public class XpStatusDto
    {
        public Guid LearnerLanguageId { get; set; }
        public Guid LanguageId { get; set; }
        public int ExperiencePoints { get; set; }
        public int TodayXp { get; set; }
        public int DailyXpGoal { get; set; }
        public DateTime LastXpResetDate { get; set; }
        public int StreakDays { get; set; }
        public int Level { get; set; }
        public double LevelProgress { get; set; } // 0..1
    }
}
