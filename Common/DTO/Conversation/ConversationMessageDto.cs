using DAL.Type;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class ConversationMessageDto
    {
        public Guid MessageId { get; set; }
        public MessageSender Sender { get; set; }
        public string MessageContent { get; set; } = string.Empty;
        public MessageType MessageType { get; set; }

   
        public string? AudioUrl { get; set; }
        public string? AudioPublicId { get; set; }
        public int? AudioDuration { get; set; }

        public int SequenceOrder { get; set; }
        public DateTime SentAt { get; set; }

    
        public bool IsVoiceMessage => MessageType == MessageType.Audio && !string.IsNullOrEmpty(AudioUrl);

        public string FormattedDuration => AudioDuration.HasValue
            ? TimeSpan.FromSeconds(AudioDuration.Value).ToString(@"mm\:ss")
            : "0:00";
        
        // NEW: Gợi ý từ đồng nghĩa cho tin nhắn của AI
        public SynonymSuggestionDto? SynonymSuggestions { get; set; }
    }
}
