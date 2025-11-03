using BLL.IServices.Subscription;
using BLL.IServices.Payment;
using Common.Constants;
using Common.DTO.Subscription;
using Common.DTO.Payment;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Subscription
{
 public class SubscriptionService : ISubscriptionService
 {
 private readonly IUnitOfWork _unit;
 private readonly IPayOSService _payOs;
 private readonly ILogger<SubscriptionService> _logger;

 public SubscriptionService(IUnitOfWork unit, IPayOSService payOs, ILogger<SubscriptionService> logger)
 {
 _unit = unit;
 _payOs = payOs;
 _logger = logger;
 }

 public async Task<SubscriptionPurchaseResponseDto> CreateSubscriptionPurchaseAsync(Guid userId, string plan)
 {
 if (!Common.Constants.SubscriptionConstants.SubscriptionQuotas.ContainsKey(plan))
 throw new ArgumentException("Invalid plan");

 var quota = SubscriptionConstants.SubscriptionQuotas[plan];
 var price = SubscriptionConstants.SubscriptionPrices[plan];

 // Create a pending UserSubscription entry
 var sub = new UserSubscription
 {
 SubscriptionID = Guid.NewGuid(),
 UserID = userId,
 SubscriptionType = plan,
 ConversationQuota = quota,
 IsActive = false,
 Price = price,
 StartDate = DateTime.UtcNow,
 EndDate = null
 };
 await _unit.UserSubscriptions.CreateAsync(sub);
 await _unit.SaveChangesAsync();

 // Create a Purchase record for subscription (no course)
 var purchase = new Purchase
 {
 PurchasesId = Guid.NewGuid(),
 UserId = userId,
 CourseId = null,
 SubscriptionId = sub.SubscriptionID,
 TotalAmount = price,
 FinalAmount = price,
 Status = DAL.Type.PurchaseStatus.Pending,
 PaymentMethod = DAL.Type.PaymentMethod.PayOS,
 CurrencyType = DAL.Type.CurrencyType.VND,
 CreatedAt = DateTime.UtcNow
 };
 await _unit.Purchases.CreateAsync(purchase);
 await _unit.SaveChangesAsync();

 // Create PayOS payment link
 var paymentDto = new CreatePaymentDto
 {
 ClassID = Guid.Empty, // not used for subscription; PayOS service ignores this
 StudentID = userId,
 Amount = price,
 Description = $"FLearn subscription {plan}",
 ItemName = $"{plan} conversations/day"
 };
 var pay = await _payOs.CreatePaymentLinkAsync(paymentDto);
 if (!pay.Success)
 throw new InvalidOperationException(pay.ErrorMessage ?? "Cannot create payment link");

 // Record PaymentTransaction linked to subscription & purchase
 var tx = new PaymentTransaction
 {
 TransactionId = Guid.NewGuid(),
 PurchaseId = purchase.PurchasesId,
 SubscriptionId = sub.SubscriptionID,
 Amount = price,
 TransactionRef = pay.TransactionId,
 TransactionStatus = DAL.Type.TransactionStatus.Pending,
 PaymentMethod = DAL.Type.PaymentMethod.PayOS,
 CurrencyType = DAL.Type.CurrencyType.VND,
 Status = true,
 CreatedAt = DateTime.UtcNow
 };
 await _unit.PaymentTransactions.CreateAsync(tx);
 await _unit.SaveChangesAsync();

 return new SubscriptionPurchaseResponseDto
 {
 TransactionId = pay.TransactionId,
 PaymentUrl = pay.PaymentUrl,
 Amount = price,
 Plan = plan,
 ExpiryTime = pay.ExpiryTime
 };
 }

 public async Task<bool> HandleCallbackAsync(SubscriptionCallbackDto callback)
 {
 try
 {
 // Find transaction by external TransactionRef
 var tx = await _unit.PaymentTransactions.FindAsync(t => t.TransactionRef == callback.TransactionId);
 if (tx == null)
 return false;

 if (callback.Status.Equals("PAID", StringComparison.OrdinalIgnoreCase))
 {
 tx.TransactionStatus = DAL.Type.TransactionStatus.Succeeded;
 tx.CompletedAt = DateTime.UtcNow;
 await _unit.PaymentTransactions.UpdateAsync(tx);

 // Mark purchase completed if exists
 if (tx.PurchaseId.HasValue)
 {
 var purchase = await _unit.Purchases.GetByIdAsync(tx.PurchaseId.Value);
 if (purchase != null)
 {
 purchase.Status = DAL.Type.PurchaseStatus.Completed;
 purchase.PaidAt = DateTime.UtcNow;
 await _unit.Purchases.UpdateAsync(purchase);
 }
 }

 if (tx.SubscriptionId.HasValue)
 {
 var sub = await _unit.UserSubscriptions.GetByIdAsync(tx.SubscriptionId.Value);
 if (sub != null)
 {
 sub.IsActive = true;
 sub.StartDate = DateTime.UtcNow;
 sub.EndDate = DateTime.UtcNow.Date.AddDays(30); // default30 days validity
 await _unit.UserSubscriptions.UpdateAsync(sub);

 // Update user's daily conversation limit according to plan
 var user = await _unit.Users.GetByIdAsync(sub.UserID);
 if (user != null)
 {
 user.DailyConversationLimit = sub.ConversationQuota; // e.g.,5 for Basic5
 user.ConversationsUsedToday = Math.Min(user.ConversationsUsedToday, user.DailyConversationLimit);
 user.LastConversationResetDate = DateTime.UtcNow.Date;
 await _unit.Users.UpdateAsync(user);
 }
 }
 }

 await _unit.SaveChangesAsync();
 return true;
 }
 else
 {
 tx.TransactionStatus = DAL.Type.TransactionStatus.Failed;
 await _unit.PaymentTransactions.UpdateAsync(tx);
 await _unit.SaveChangesAsync();
 return false;
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error processing subscription callback");
 return false;
 }
 }
 }
}
