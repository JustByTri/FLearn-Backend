// Pseudocode:
// - Implement a BackgroundService that:
//   - On startup: run a reset to catch missed midnights.
//   - Compute delay until next Vietnam midnight (00:00).
//   - Await the delay, then reset daily counters for all users:
//       * ConversationsUsedToday = 0
//       * LastConversationResetDate = now (Vietnam time)
//   - Save changes via UnitOfWork.
//   - Loop to schedule the next midnight.
// - Include error handling and cancellation token support.

using System;
using System.Threading;
using System.Threading.Tasks;
using DAL.UnitOfWork;
using DAL.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BLL.HostedServices
{
    public sealed class DailyConversationResetService : BackgroundService
    {
        private readonly IUnitOfWork _unit;
        private readonly ILogger<DailyConversationResetService> _logger;

        public DailyConversationResetService(IUnitOfWork unit, ILogger<DailyConversationResetService> logger)
        {
            _unit = unit;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
                var nowVn = TimeHelper.GetVietnamTime();
                var users = await _unit.Users.GetAllAsync();

                var updated = 0;
                foreach (var user in users)
                {
                    if (user == null) continue;

                    // Reset once per day at 00:00 (VN time)
                    if (user.LastConversationResetDate.Date != nowVn.Date)
                    {
                        user.ConversationsUsedToday = 0;
                        user.LastConversationResetDate = nowVn;
                        await _unit.Users.UpdateAsync(user);
                        updated++;
                    }
                }

                if (updated > 0)
                {
                    await _unit.SaveChangesAsync();
                    _logger.LogInformation("Daily conversation reset completed at {Time}. Users updated: {Count}", nowVn, updated);
                }
                else
                {
                    _logger.LogInformation("Daily conversation reset skipped at {Time}. No users required update.", nowVn);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error performing daily conversation reset.");
            }
        }
    }
}