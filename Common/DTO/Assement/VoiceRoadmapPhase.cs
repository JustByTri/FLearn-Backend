using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceRoadmapPhase
    {
        public int PhaseNumber { get; set; }
        public string Title { get; set; } = string.Empty;
        public string FocusArea { get; set; } = string.Empty; 

        public string Duration { get; set; } = string.Empty;
        public List<string> Goals { get; set; } = new();
        public List<string> PracticeActivities { get; set; } = new();
        public List<string> Resources { get; set; } = new();
    }
}
