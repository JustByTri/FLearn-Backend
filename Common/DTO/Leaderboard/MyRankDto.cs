using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Leaderboard
{
    public class MyRankDto
    {
        public Guid LanguageId { get; set; }
        public int StreakDays { get; set; } 
        public int Rank { get; set; }
        public string Level { get; set; } = string.Empty;
    }
}
