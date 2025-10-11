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
        public float OverallScore { get; set; }
        public float FluentScore { get; set; }
        public float GrammarScore { get; set; }
        public float VocabularyScore { get; set; }
        public float CulturalScore { get; set; }
        public string AIFeedback { get; set; } = string.Empty;
        public string Improvements { get; set; } = string.Empty;
        public string Strengths { get; set; } = string.Empty;
        public int TotalMessages { get; set; }
        public int SessionDuration { get; set; }
    }
}
