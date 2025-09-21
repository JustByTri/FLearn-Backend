using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Auth
{
    public interface IEmailService
    {
        Task<bool> SendWelcomeEmailAsync(string toEmail, string userName);
        Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken);
        Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string confirmationToken);

        /// <summary>
        /// Gửi email thông báo đã nhận được đơn ứng tuyển giáo viên
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="userName"></param>
        /// <returns></returns>
        Task<bool> SendTeacherApplicationSubmittedAsync(string toEmail, string userName);
        Task<bool> SendTeacherApplicationApprovedAsync(string toEmail, string userName);
        Task<bool> SendTeacherApplicationRejectedAsync(string toEmail, string userName, string reason);
    }
}
