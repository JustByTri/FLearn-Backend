using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceLearningRoadmapDto
    {
        public string CurrentLevel { get; set; } = string.Empty;
        public string TargetLevel { get; set; } = string.Empty;
        public List<VoiceRoadmapPhase> Phases { get; set; } = new();
        public List<string> RecommendedSpeakingCourses { get; set; } = new();
        public List<string> VocalPracticeTips { get; set; } = new();
    }
}
