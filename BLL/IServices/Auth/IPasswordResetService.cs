using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Auth
{
    public interface IPasswordResetService
    {
        Task<bool> SendPasswordResetOtpAsync(string email, string ipAddress, string userAgent);
        Task<bool> VerifyPasswordResetOtpAsync(string email, string otpCode, string ipAddress);
        Task<bool> ResetPasswordWithOtpAsync(string email, string otpCode, string newPassword);
    }
}
