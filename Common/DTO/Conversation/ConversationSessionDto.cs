using DAL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class ConversationSessionDto
    {
        public Guid SessionId { get; set; }
        public string SessionName { get; set; } = string.Empty;
        public string LanguageName { get; set; } = string.Empty;
        public string TopicName { get; set; } = string.Empty;
        public string DifficultyLevel { get; set; } = string.Empty;
        public string CharacterRole { get; set; } = string.Empty;
        public string ScenarioDescription { get; set; } = string.Empty;
        public List<ConversationMessageDto> Messages { get; set; } = new();
        public ConversationSessionStatus Status { get; set; }
        public DateTime StartedAt { get; set; }
        public float? OverallScore { get; set; }
        public string? AIFeedback { get; set; }
        public List<ConversationTaskDto> Tasks { get; set; } = new List<ConversationTaskDto>();
    }
}
