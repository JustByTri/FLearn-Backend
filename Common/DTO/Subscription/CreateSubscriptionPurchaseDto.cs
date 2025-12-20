using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Subscription
{
    public class CreateSubscriptionPurchaseDto
    {
        [Required]
        public int PlanId { get; set; }
    }
}
