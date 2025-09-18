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
    public class OtpCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OtpCleanupService> _logger;

        public OtpCleanupService(IServiceProvider serviceProvider, ILogger<OtpCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                      
                        var expiredOtps = await unitOfWork.RegistrationOtps.GetAllAsync();
                        var expiredOtpList = expiredOtps.Where(otp => otp.ExpireAt <= DateTime.UtcNow).ToList();

                        if (expiredOtpList.Any())
                        {
                            foreach (var otp in expiredOtpList)
                            {
                                await unitOfWork.RegistrationOtps.RemoveAsync(otp);
                            }

                            _logger.LogInformation($"Cleaned up {expiredOtpList.Count} expired OTP records");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while cleaning up expired OTPs");
                }

                // Chạy mỗi 10 phút
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
