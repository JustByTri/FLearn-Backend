using System;

namespace Common.DTO.Subscription
{
 public class SubscriptionPurchaseResponseDto
 {
 public string TransactionId { get; set; } = string.Empty;
 public string PaymentUrl { get; set; } = string.Empty;
 public decimal Amount { get; set; }
 public string Plan { get; set; } = string.Empty;
 public DateTime ExpiryTime { get; set; }
 }
}
