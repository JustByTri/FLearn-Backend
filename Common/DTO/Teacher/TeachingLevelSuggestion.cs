using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class TeachingLevelSuggestion
    {
        public string Level { get; set; } = string.Empty; 
        public int ConfidenceScore { get; set; } 
        public string Justification { get; set; } = string.Empty;
        public bool IsRecommended { get; set; }
    }
}
