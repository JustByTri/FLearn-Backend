using BLL.IServices.Payment;
using BLL.IServices.Subscription;
using Common.DTO.Payment;
using Common.DTO.Subscription;
using DAL.Helpers;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
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

        public async Task<SubscriptionPurchaseResponseDto> CreateSubscriptionPurchaseAsync(Guid userId, int subscriptionPlanId)
        {
            var planEntity = await _unit.Subscriptions.Query().Where(s => s.SubscriptionId == subscriptionPlanId).FirstOrDefaultAsync();

            if (planEntity == null)
                throw new ArgumentException("Subscription plan not found");

            var price = planEntity.Price;
            var quota = planEntity.ConversationQuota;
            var planName = planEntity.Name;

            // Create a pending UserSubscription entry
            var sub = new UserSubscription
            {
                SubscriptionID = Guid.NewGuid(),
                UserID = userId,
                SubscriptionType = planName,
                ConversationQuota = quota,
                IsActive = false,
                Price = price,
                StartDate = TimeHelper.GetVietnamTime(),
                EndDate = null
            };

            await _unit.UserSubscriptions.CreateAsync(sub);
            await _unit.SaveChangesAsync();

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
                CreatedAt = TimeHelper.GetVietnamTime()
            };
            await _unit.Purchases.CreateAsync(purchase);
            await _unit.SaveChangesAsync();

            // Create PayOS payment link
            var paymentDto = new CreatePaymentDto
            {
                ClassID = Guid.Empty,
                StudentID = userId,
                Amount = price,
                Description = $"Nâng cấp gói {planName}",
                ItemName = $"Gói: {planName} ({quota} cuộc hội thoại/ngày)"
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
                CreatedAt = TimeHelper.GetVietnamTime()
            };
            await _unit.PaymentTransactions.CreateAsync(tx);
            await _unit.SaveChangesAsync();

            return new SubscriptionPurchaseResponseDto
            {
                TransactionId = pay.TransactionId,
                PaymentUrl = pay.PaymentUrl,
                Amount = price,
                Plan = planName,
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
                    tx.CompletedAt = TimeHelper.GetVietnamTime();
                    await _unit.PaymentTransactions.UpdateAsync(tx);

                    // Mark purchase completed if exists
                    if (tx.PurchaseId.HasValue)
                    {
                        var purchase = await _unit.Purchases.GetByIdAsync(tx.PurchaseId.Value);
                        if (purchase != null)
                        {
                            purchase.Status = DAL.Type.PurchaseStatus.Completed;
                            purchase.PaidAt = TimeHelper.GetVietnamTime();
                            await _unit.Purchases.UpdateAsync(purchase);
                        }
                    }

                    if (tx.SubscriptionId.HasValue)
                    {
                        var sub = await _unit.UserSubscriptions.GetByIdAsync(tx.SubscriptionId.Value);
                        if (sub != null)
                        {
                            // Activate the new subscription
                            var nowVn = TimeHelper.GetVietnamTime();
                            sub.IsActive = true;
                            sub.StartDate = nowVn;
                            // Set end date to the same time next month
                            sub.EndDate = nowVn.AddMonths(1);
                            await _unit.UserSubscriptions.UpdateAsync(sub);

                            // Deactivate other active subscriptions of this user
                            var allSubs = await _unit.UserSubscriptions.GetAllAsync();
                            foreach (var other in allSubs.Where(s => s.UserID == sub.UserID && s.IsActive && s.SubscriptionID != sub.SubscriptionID))
                            {
                                other.IsActive = false;
                                other.EndDate = TimeHelper.GetVietnamTime();
                                await _unit.UserSubscriptions.UpdateAsync(other);
                            }

                            // Update user's daily conversation limit according to new plan
                            var user = await _unit.Users.GetByIdAsync(sub.UserID);
                            if (user != null)
                            {
                                user.DailyConversationLimit = sub.ConversationQuota;
                                user.ConversationsUsedToday = 0;
                                user.LastConversationResetDate = TimeHelper.GetVietnamTime();
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
