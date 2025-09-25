using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Teacher
{
    public class TeacherQualificationAnalysisDto
    {
        public Guid ApplicationId { get; set; }
        public string LanguageName { get; set; } = string.Empty;
        public List<string> SuggestedTeachingLevels { get; set; } = new();
        public int ConfidenceScore { get; set; } 
        public string ReasoningExplanation { get; set; } = string.Empty;
        public List<QualificationAssessment> QualificationAssessments { get; set; } = new();
        public string OverallRecommendation { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
    }
}
