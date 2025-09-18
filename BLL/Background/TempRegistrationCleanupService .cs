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
    public class TempRegistrationCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<TempRegistrationCleanupService> _logger;

        public TempRegistrationCleanupService(IServiceProvider serviceProvider, ILogger<TempRegistrationCleanupService> logger)
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

                        var expiredTempRegs = await unitOfWork.TempRegistrations.GetAllAsync();
                        var expiredList = expiredTempRegs.Where(temp => temp.ExpireAt <= DateTime.UtcNow).ToList();

                        if (expiredList.Any())
                        {
                            foreach (var temp in expiredList)
                            {
                                await unitOfWork.TempRegistrations.RemoveAsync(temp);
                            }

                            _logger.LogInformation($"Đã dọn dẹp {expiredList.Count} bản ghi đăng ký tạm thời hết hạn");
                        }

                     
                        try
                        {
                            var expiredOtps = await unitOfWork.RegistrationOtps.GetAllAsync();
                            var expiredOtpList = expiredOtps.Where(otp => otp.ExpireAt <= DateTime.UtcNow).ToList();

                            if (expiredOtpList.Any())
                            {
                                foreach (var otp in expiredOtpList)
                                {
                                    await unitOfWork.RegistrationOtps.RemoveAsync(otp);
                                }

                                _logger.LogInformation($"Đã dọn dẹp {expiredOtpList.Count} mã OTP hết hạn");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Lỗi khi dọn dẹp OTP (có thể chưa được cấu hình)");
                        }

                        await unitOfWork.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Lỗi xảy ra trong quá trình dọn dẹp dữ liệu tạm thời");
                }

                // Chạy mỗi 10 phút
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
