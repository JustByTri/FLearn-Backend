using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Assement
{
    public class VoiceAssessmentResultDto
    {
        public Guid AssessmentId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public Guid LaguageID { get; set; }

        public Guid LearnerLanguageId { get; set; } 
        public Guid? ProgramId { get; set; } 
        public string? ProgramName { get; set; } 

      
        public string DeterminedLevel { get; set; } = string.Empty; 
        public int LevelConfidence { get; set; } 
        public string AssessmentCompleteness { get; set; } = string.Empty;

  
        public int OverallScore { get; set; }
        public int PronunciationScore { get; set; }
        public int FluencyScore { get; set; }
        public int GrammarScore { get; set; }
        public int VocabularyScore { get; set; }

    
        public string DetailedFeedback { get; set; } = string.Empty;
        public List<string> KeyStrengths { get; set; } = new();
        public List<string> ImprovementAreas { get; set; } = new();
        public string NextLevelRequirements { get; set; } = string.Empty;

    
  
        public List<RecommendedCourseDto>? RecommendedCourses { get; set; } 

        public DateTime CompletedAt { get; set; }
    }
}
