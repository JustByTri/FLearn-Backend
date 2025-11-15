using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class ConversationEvaluationDto
    {
        public Guid SessionId { get; set; }
        
        // Giữ lại điểm số để tương thích
        public float OverallScore { get; set; }
        public float FluentScore { get; set; }
        public float GrammarScore { get; set; }
        public float VocabularyScore { get; set; }
        public float CulturalScore { get; set; }
        
        // Giữ lại các field cũ
        public string AIFeedback { get; set; } = string.Empty;
        public string Improvements { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        
        public int TotalMessages { get; set; }
        public int SessionDuration { get; set; }
        
        // NEW: Detailed analysis
        public DetailedSkillAnalysis? FluentAnalysis { get; set; }
        public DetailedSkillAnalysis? GrammarAnalysis { get; set; }
        public DetailedSkillAnalysis? VocabularyAnalysis { get; set; }
        public DetailedSkillAnalysis? CulturalAnalysis { get; set; }
        
        public List<SpecificObservation>? SpecificObservations { get; set; }
        public List<string>? PositivePatterns { get; set; }
        public List<string>? AreasNeedingWork { get; set; }
        public string? ProgressSummary { get; set; }
    }
}
