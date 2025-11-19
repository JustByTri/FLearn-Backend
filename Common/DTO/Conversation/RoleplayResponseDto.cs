using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class RoleplayResponseDto
    {
        public string Content { get; set; } // Lời thoại (hoặc lời nhắc)
        public bool IsOffTopic { get; set; } // True nếu user nói lạc đề/nonsense
        public bool IsTaskCompleted { get; set; } // True nếu user hoàn thành nhiệm vụ
        public bool IsConversationFinished { get; set; } // True nếu kịch bản kết thúc
    }
}
