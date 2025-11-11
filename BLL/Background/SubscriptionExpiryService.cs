using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.HostedServices
{
 /// <summary>
 /// Hosted service that periodically deactivates expired subscriptions (EndDate < now) and resets user limits to Free plan.
 /// Runs every hour; on startup performs an immediate check.
 /// </summary>
 public sealed class SubscriptionExpiryService : BackgroundService
 {
 private readonly IUnitOfWork _unit;
 private readonly ILogger<SubscriptionExpiryService> _logger;

 public SubscriptionExpiryService(IUnitOfWork unit, ILogger<SubscriptionExpiryService> logger)
 {
 _unit = unit;
 _logger = logger;
 }

 protected override async Task ExecuteAsync(CancellationToken stoppingToken)
 {
 await SafeCheckAsync(stoppingToken);
 while (!stoppingToken.IsCancellationRequested)
 {
 try
 {
 await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
 await SafeCheckAsync(stoppingToken);
 }
 catch (TaskCanceledException)
 {
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error in subscription expiry loop");
 await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
 }
 }
 }

 private async Task SafeCheckAsync(CancellationToken ct)
 {
 try
 {
 var now = TimeHelper.GetVietnamTime();
 var subs = await _unit.UserSubscriptions.GetAllAsync();
 int changed =0;
 foreach (var sub in subs.Where(s => s.IsActive && s.EndDate.HasValue && s.EndDate.Value <= now))
 {
 sub.IsActive = false;
 // keep EndDate as is (expiry moment)
 await _unit.UserSubscriptions.UpdateAsync(sub);
 changed++;

 // Downgrade user to Free quota if no other active subscription remains
 var userSubs = subs.Where(s => s.UserID == sub.UserID && s.IsActive && s.SubscriptionID != sub.SubscriptionID).ToList();
 if (!userSubs.Any())
 {
 var user = await _unit.Users.GetByIdAsync(sub.UserID);
 if (user != null)
 {
 user.DailyConversationLimit = Common.Constants.SubscriptionConstants.SubscriptionQuotas[Common.Constants.SubscriptionConstants.FREE];
 // do not reset ConversationsUsedToday here; DailyConversationResetService will handle midnight reset
 await _unit.Users.UpdateAsync(user);
 }
 }
 }

 if (changed >0)
 {
 await _unit.SaveChangesAsync();
 _logger.LogInformation("Subscription expiry check: {Count} subscriptions deactivated at {Time}", changed, now);
 }
 }
 catch (Exception ex)
 {
 _logger.LogError(ex, "Error executing subscription expiry check");
 }
 }
 }
}
