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

        // Extended subscription/usage info
        public int RemainingToday => Math.Max(0, DailyLimit - ConversationsUsedToday);
        public bool HasActiveSubscription { get; set; }
        public string? CurrentPlan { get; set; }
        public int? PlanDailyQuota { get; set; }
        public decimal? PlanPrice { get; set; }
        public string? PlanPriceVndFormatted { get; set; }
        public DateTime? PlanStartDate { get; set; }
        public DateTime? PlanEndDate { get; set; }
        public int DaysUntilReset => (int)Math.Ceiling((ResetDate - DateTime.UtcNow).TotalDays);
    }
}
