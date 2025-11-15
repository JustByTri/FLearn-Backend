using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Gamification
{
    public class UpdateDailyGoalDto
    {
        [Range(10,2000)]
        public int DailyXpGoal { get; set; }
    }
}
