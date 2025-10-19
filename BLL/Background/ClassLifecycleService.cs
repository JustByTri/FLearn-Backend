using DAL.UnitOfWork;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Background
{
    public class ClassLifecycleService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ClassLifecycleService> _logger;

        public ClassLifecycleService(IServiceProvider serviceProvider, ILogger<ClassLifecycleService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                // 1. Auto-cancel classes with insufficient students before cutoff
                await AutoCancelInsufficientClasses(unitOfWork);

                // 2. Handle dispute window and payouts
                await HandleDisputeAndPayouts(unitOfWork);

                await Task.Delay(TimeSpan.FromHours(1), stoppingToken); // Run hourly
            }
        }

        private async Task AutoCancelInsufficientClasses(IUnitOfWork unitOfWork)
        {
            // Find classes where start time is within cutoff and enrolled < min_students
            // Set status to Cancelled_InsufficientStudents, refund enrollments
            // Use PayOSService.RefundPaymentAsync for each enrollment
        }

        private async Task HandleDisputeAndPayouts(IUnitOfWork unitOfWork)
        {
            // For finished classes, open dispute window, then mark Completed_PendingPayout
            // After dispute window, create payout record (90% to teacher, 10% to platform)
            // If dispute exists, hold payout until resolved
        }
    }
}
