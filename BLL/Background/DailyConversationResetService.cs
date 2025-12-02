// - Implement a BackgroundService that:
// - On startup: run a reset to catch missed midnights.
// - Compute delay until next Vietnam midnight (00:00).
// - Await the delay, then reset daily counters for all users:
// * ConversationsUsedToday =0
// * LastConversationResetDate = now (Vietnam time)
// * For LearnerLanguage: TodayXp =0 and update streak if goal met
// - Save changes via UnitOfWork.
// - Loop to schedule the next midnight.
// - Include error handling and cancellation token support.

using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.HostedServices
{
    public sealed class DailyConversationResetService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DailyConversationResetService> _logger;

        public DailyConversationResetService(IServiceScopeFactory scopeFactory, ILogger<DailyConversationResetService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.Yield();

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
            catch (TaskCanceledException) { return; }

            // Ensure counters are correct if the app restarts after midnight
            await SafeResetAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var delay = GetDelayUntilNextVietnamMidnight();
                    await Task.Delay(delay, stoppingToken);

                    await SafeResetAsync(stoppingToken);
                }
                catch (TaskCanceledException)
                {
                    // Application is shutting down
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error in DailyConversationResetService loop.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private static TimeSpan GetDelayUntilNextVietnamMidnight()
        {
            var nowVn = TimeHelper.GetVietnamTime();
            var nextMidnightVn = nowVn.Date.AddDays(1);
            var delay = nextMidnightVn - nowVn;
            return delay < TimeSpan.FromSeconds(1) ? TimeSpan.FromSeconds(1) : delay;
        }

        private async Task SafeResetAsync(CancellationToken ct)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var unit = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var nowVn = TimeHelper.GetVietnamTime();
                var users = await unit.Users.GetAllAsync();
                var llAll = await unit.LearnerLanguages.GetAllAsync();

                var updatedUsers = 0;
                var updatedLearners = 0;
                foreach (var user in users)
                {
                    if (user == null) continue;

                    // Reset once per day at00:00 (VN time)
                    if (user.LastConversationResetDate.Date != nowVn.Date)
                    {
                        user.ConversationsUsedToday = 0;
                        user.LastConversationResetDate = nowVn;
                        await unit.Users.UpdateAsync(user);
                        updatedUsers++;
                    }
                }

                foreach (var ll in llAll)
                {
                    if (ll == null) continue;
                    if (ll.LastXpResetDate.Date != nowVn.Date)
                    {
                        // Update streak if met daily goal yesterday and not already incremented
                        if (ll.TodayXp >= ll.DailyXpGoal && (ll.StreakDaysUpdatedAt == null || ll.StreakDaysUpdatedAt.Value.Date != nowVn.Date))
                        {
                            ll.StreakDays += 1;
                            ll.StreakDaysUpdatedAt = nowVn.Date;
                        }
                        else if (ll.TodayXp < ll.DailyXpGoal)
                        {
                            ll.StreakDays = 0;
                            ll.StreakDaysUpdatedAt = null;
                        }
                        ll.TodayXp = 0;
                        ll.LastXpResetDate = nowVn;
                        await unit.LearnerLanguages.UpdateAsync(ll);
                        updatedLearners++;
                    }
                }

                if (updatedUsers + updatedLearners > 0)
                {
                    await unit.SaveChangesAsync();
                    _logger.LogInformation("Daily reset completed at {Time}. Users updated: {Users}, Learners updated: {Learners}", nowVn, updatedUsers, updatedLearners);
                }
                else
                {
                    _logger.LogInformation("Daily reset skipped at {Time}. No entities required update.", nowVn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing daily reset.");
            }
        }
    }
}