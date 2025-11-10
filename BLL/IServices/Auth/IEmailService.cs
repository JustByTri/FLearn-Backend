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
        /// <summary>
        /// Gửi Lại OTP và OTP Password
        /// </summary>
        /// <param name="toEmail"></param>
        /// <param name="userName"></param>
        /// <param name="otpCode"></param>
        /// <returns></returns>
        Task<bool> SendOtpResendAsync(string toEmail, string userName, string otpCode);
        Task<bool> SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode);
        
    // ================== REFUND REQUEST EMAILS ==================
        
        /// <summary>
        /// Admin gửi email thông báo học viên cần làm đơn hoàn tiền
        /// </summary>
        Task<bool> SendRefundRequestInstructionAsync(
            string toEmail,
            string userName,
   string className,
    DateTime classStartDateTime,
     string? reason = null);

     /// <summary>
        /// Gửi email xác nhận đã nhận đơn hoàn tiền
        /// </summary>
        Task<bool> SendRefundRequestConfirmationAsync(
  string toEmail,
     string userName,
   string className,
            string refundRequestId);

     /// <summary>
  /// Gửi email thông báo đơn hoàn tiền đã được chấp nhận
        /// </summary>
        Task<bool> SendRefundRequestApprovedAsync(
 string toEmail,
        string userName,
         string className,
            decimal refundAmount,
     string? proofImageUrl = null,
    string? adminNote = null);

      /// <summary>
        /// Gửi email thông báo đơn hoàn tiền bị từ chối
        /// </summary>
  Task<bool> SendRefundRequestRejectedAsync(
            string toEmail,
         string userName,
            string className,
      string rejectionReason);

    // ================== PAYOUT REQUEST EMAILS ==================
  
      /// <summary>
     /// Gửi email thông báo yêu cầu rút tiền đã được duyệt
        /// </summary>
     Task<bool> SendPayoutRequestApprovedAsync(
  string toEmail,
          string teacherName,
decimal amount,
         string bankName,
       string accountNumber,
     string? transactionRef = null,
            string? adminNote = null);

        /// <summary>
        /// Gửi email thông báo yêu cầu rút tiền bị từ chối
      /// </summary>
        Task<bool> SendPayoutRequestRejectedAsync(
   string toEmail,
     string teacherName,
            decimal amount,
            string rejectionReason);
    }
}
