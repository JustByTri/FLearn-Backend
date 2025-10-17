using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common.DTO.Conversation
{
    public class ConversationUsageDto
    {
        public int ConversationsUsedToday { get; set; }
        public int DailyLimit { get; set; }
        public string SubscriptionType { get; set; }
        public DateTime ResetDate { get; set; }
    }
}
