using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class GeneratedConversationContentDto
    {
        public string ScenarioDescription { get; set; } = string.Empty;
        public string AIRole { get; set; } = string.Empty;
        public string SystemPrompt { get; set; } = string.Empty;
        public string FirstMessage { get; set; } = string.Empty;

        public List<ConversationTaskDto> Tasks { get; set; } = new List<ConversationTaskDto>();
    }
}
