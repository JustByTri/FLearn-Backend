using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class ConversationContextDto
    {
        public string Language { get; set; } = string.Empty;
        public string LanguageCode { get; set; } = string.Empty;
        public string Topic { get; set; } = string.Empty;
        public string TopicDescription { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public string MasterPrompt { get; set; } = string.Empty;
        public string ScenarioGuidelines { get; set; } = string.Empty;
        public string RoleplayInstructions { get; set; } = string.Empty;
        public string EvaluationCriteria { get; set; } = string.Empty;
    }
}
