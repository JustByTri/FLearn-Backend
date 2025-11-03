using System.ComponentModel.DataAnnotations;

namespace Common.DTO.Subscription
{
 public class SubscriptionCallbackDto
 {
 [Required]
 public string TransactionId { get; set; } = string.Empty;
 [Required]
 public string Status { get; set; } = string.Empty; // PAID | CANCELLED | EXPIRED
 public decimal Amount { get; set; }
 public string? Signature { get; set; }
 public string? Plan { get; set; }
 }
}
