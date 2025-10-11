using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class StartConversationRequestDto
    {
        public Guid LanguageId { get; set; }
        public Guid TopicId { get; set; }
        public string DifficultyLevel { get; set; } = string.Empty;
    }
}
