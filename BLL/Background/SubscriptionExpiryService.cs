using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BLL.HostedServices
{
 /// <summary>
 /// Hosted service that periodically deactivates expired subscriptions (EndDate < now) and resets user limits to Free plan.
 /// Runs every hour; on startup performs an immediate check.
 /// </summary>
 public sealed class SubscriptionExpiryService : BackgroundService
 {
 private readonly IServiceScopeFactory _scopeFactory;
 private readonly ILogger<SubscriptionExpiryService> _logger;

 public SubscriptionExpiryService(IServiceScopeFactory scopeFactory, ILogger<SubscriptionExpiryService> logger)
 {
 _scopeFactory = scopeFactory;
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
 using var scope = _scopeFactory.CreateScope();
 var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

 var now = TimeHelper.GetVietnamTime();
 var subs = await unit.UserSubscriptions.GetAllAsync();
 int changed =0;
 foreach (var sub in subs.Where(s => s.IsActive && s.EndDate.HasValue && s.EndDate.Value <= now))
 {
 sub.IsActive = false;
 // keep EndDate as is (expiry moment)
 await unit.UserSubscriptions.UpdateAsync(sub);
 changed++;

 // Downgrade user to Free quota if no other active subscription remains
 var userHasOtherActive = subs.Any(s => s.UserID == sub.UserID && s.IsActive);
 if (!userHasOtherActive)
 {
 var user = await unit.Users.GetByIdAsync(sub.UserID);
 if (user != null)
 {
 user.DailyConversationLimit = Common.Constants.SubscriptionConstants.SubscriptionQuotas[Common.Constants.SubscriptionConstants.FREE];
 await unit.Users.UpdateAsync(user);
 }
 }
 }

 if (changed >0)
 {
 await unit.SaveChangesAsync();
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
