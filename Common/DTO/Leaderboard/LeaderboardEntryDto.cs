using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Leaderboard
{
    public class LeaderboardEntryDto
    {
        public int Rank { get; set; } 
        public Guid LearnerId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
        public int StreakDays { get; set; }
        public string Level { get; set; } = string.Empty; 
    }
}
