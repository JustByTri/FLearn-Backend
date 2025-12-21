namespace Common.DTO.Subscription.Response
{
    public class SubscriptionPlanResponse
    {
        public int SubscriptionId { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int ConversationQuota { get; set; }
        public bool IsCurrentPlan { get; set; } = false;
        public string CreatedAt { get; set; }
        public string UpdatedAt { get; set; }
    }
}
