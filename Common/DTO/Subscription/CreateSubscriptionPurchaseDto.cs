using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Subscription
{
 public class CreateSubscriptionPurchaseDto
 {
 [Required]
 public string Plan { get; set; } = string.Empty; // Basic5 | Basic10 | Basic15
 }
}
