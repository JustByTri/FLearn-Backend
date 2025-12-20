using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Subscription.Request
{
    public class SubscriptionPlanRequest
    {
        [Required(ErrorMessage = "Plan name is required")]
        public string Name { get; set; }
        [Required(ErrorMessage = "Price is required")]
        [Range(5_000, 500_000, ErrorMessage = "Price must be between 5,000 and 5,000,000")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Conversation quota is required")]
        [Range(1, 100, ErrorMessage = "Quota must be between 1 and 100")]
        public int ConversationQuota { get; set; }
    }
}
