using BLL.IServices.Auth;
using DAL.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BLL.Services.Auth
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        // --- BRAND COLORS (Ocean Blue Theme) ---
        private const string PrimaryColor = "#0052CC";
        private const string SecondaryColor = "#2684FF";
        private const string SuccessColor = "#36B37E";
        private const string WarningColor = "#FFAB00";
        private const string DangerColor = "#FF5630";

        // Text Colors
        private const string TextDark = "#091E42"; // Xanh đen đậm
        private const string TextLight = "#505F79"; // Xám xanh trung tính
        private const string BgBody = "#F4F5F7";

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        // --- 1. AUTHENTICATION EMAILS ---

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            var subject = "Chào mừng gia nhập Flearn!";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='margin-top: 20px;'>Xin chào, {userName}!</h2>
                    <p class='lead-text'>
                        Cảm ơn bạn đã chọn <strong>Flearn</strong>. Chúng tôi đã sẵn sàng cùng bạn chinh phục những mục tiêu ngôn ngữ mới.
                    </p>
                    <div style='margin: 40px 0;'>
                        <a href='{_configuration["AppSettings:BaseUrl"]}' style='{GetButtonStyle()}'>
                            Bắt đầu hành trình
                        </a>
                    </div>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Welcome to Flearn", content));
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
        {
            var resetLink = $"{_configuration["AppSettings:BaseUrl"]}/reset-password?token={resetToken}";
            var subject = "Yêu cầu đặt lại mật khẩu";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='margin-top: 20px;'>Đặt lại mật khẩu</h2>
                    <p style='color: {TextLight}; font-size: 16px;'>Chúng tôi nhận được yêu cầu thay đổi mật khẩu cho tài khoản <strong>{userName}</strong>.</p>
                    
                    <div style='margin: 40px 0;'>
                        <a href='{resetLink}' style='{GetButtonStyle(DangerColor)}'>
                            Thiết lập mật khẩu mới
                        </a>
                    </div>
                    <p style='color: {TextLight}; font-size: 14px;'>Link này chỉ có hiệu lực trong 24 giờ.</p>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Bảo mật tài khoản", content));
        }

        public async Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string otpCode)
        {
            return await SendOtpEmailBase(toEmail, userName, otpCode, "Xác thực tài khoản");
        }

        public async Task<bool> SendOtpResendAsync(string toEmail, string userName, string otpCode)
        {
            return await SendOtpEmailBase(toEmail, userName, otpCode, "Mã xác thực mới");
        }

        public async Task<bool> SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode)
        {
            return await SendOtpEmailBase(toEmail, userName, otpCode, "Quên mật khẩu", true);
        }

        // --- 2. TEACHER & APPLICATION EMAILS ---

        public async Task<bool> SendTeacherApplicationSubmittedAsync(string toEmail, string userName)
        {
            var subject = "Đã nhận đơn ứng tuyển";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='margin-top: 20px;'>Đang xét duyệt hồ sơ</h2>
                    <p style='color: {TextLight}; font-size: 16px;'>Chào {userName}, cảm ơn bạn đã quan tâm đến vị trí Giáo viên tại Flearn.</p>
                    
                    <div style='margin: 30px 0;'>
                        <div style='background-color: #DEEBFF; color: {PrimaryColor}; padding: 20px 40px; border-radius: 8px; font-weight: 700; font-size: 18px; display: inline-block;'>
                            Dự kiến phản hồi: 3-5 ngày làm việc
                        </div>
                    </div>
                    
                    <p style='color: {TextLight}; font-size: 14px;'>Vui lòng kiểm tra email thường xuyên để nhận cập nhật.</p>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Tuyển dụng", content));
        }

        public async Task<bool> SendTeacherApplicationApprovedAsync(string toEmail, string userName)
        {
            var subject = "Chúc mừng Giáo viên mới!";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {SuccessColor}; margin-top: 20px;'>Chúc mừng {userName}!</h2>
                    <p class='lead-text'>Hồ sơ của bạn đã được chấp thuận.</p>
                    <p style='color: {TextLight};'>Giờ đây bạn đã là một phần của đội ngũ giáo viên Flearn. Hãy bắt đầu tạo khóa học đầu tiên ngay thôi.</p>
                    
                    <div style='margin: 40px 0;'>
                        <a href='{_configuration["AppSettings:BaseUrl"]}/teacher/dashboard' style='{GetButtonStyle(SuccessColor)}'>
                            Vào Dashboard Giáo viên
                        </a>
                    </div>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Kết quả ứng tuyển", content));
        }

        public async Task<bool> SendTeacherApplicationRejectedAsync(string toEmail, string userName, string reason)
        {
            var subject = "Thông báo kết quả ứng tuyển";

            var content = $@"
                <div>
                    <h2 style='margin-top: 10px;'>Thông báo kết quả</h2>
                    <p style='color: {TextLight};'>Chào {userName}, cảm ơn bạn đã dành thời gian ứng tuyển.</p>
                    <p style='color: {TextLight};'>Rất tiếc, sau khi xem xét kỹ lưỡng, hồ sơ của bạn chưa phù hợp ở thời điểm hiện tại.</p>
                    
                    <div style='border-left: 4px solid {DangerColor}; background: #FFF5F5; padding: 25px; border-radius: 0 8px 8px 0; margin: 30px 0;'>
                        <div style='font-size: 12px; color: {DangerColor}; font-weight: 800; text-transform: uppercase; letter-spacing: 1px; margin-bottom: 10px;'>Lý do từ chối</div>
                        <div style='color: {TextDark}; font-weight: 600; font-size: 16px;'>{reason}</div>
                    </div>
                    <p style='color: {TextLight}; font-size: 14px;'>Cánh cửa Flearn vẫn luôn mở, bạn có thể cập nhật hồ sơ và thử lại sau.</p>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Tuyển dụng", content));
        }

        // --- 3. ACCOUNT & TRANSACTIONS ---

        public async Task SendBanNotificationAsync(string toEmail, string fullName, string reason)
        {
            var subject = "⚠️ Thông báo tạm khóa tài khoản";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {DangerColor}; margin-top: 20px;'>Tài khoản bị tạm dừng</h2>
                    <p style='color: {TextLight}; margin-bottom: 30px;'>Chúng tôi đã phát hiện hoạt động vi phạm chính sách trên tài khoản <strong>{fullName}</strong>.</p>
                    
                    <div style='background-color: #FFF5F5; padding: 25px; border-radius: 12px; margin: 0 auto; text-align: left; border: 1px solid #FFBDAD;'>
                        <div style='color: {DangerColor}; font-weight: 700; text-transform: uppercase; font-size: 12px; margin-bottom: 8px;'>Nguyên nhân cụ thể</div>
                        <div style='color: {TextDark}; font-weight: 600; font-size: 16px;'>{reason}</div>
                    </div>
                    
                    <p style='color: {TextLight}; font-size: 14px; margin-top: 30px;'>Nếu bạn cho rằng đây là sự nhầm lẫn, vui lòng liên hệ bộ phận hỗ trợ.</p>
                </div>";

            await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Thông báo vi phạm", content));
        }

        public async Task SendUnbanNotificationAsync(string toEmail, string fullName)
        {
            var subject = "Tài khoản đã được mở lại";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {SuccessColor}; margin-top: 20px;'>Khôi phục thành công!</h2>
                    <p class='lead-text'>Chào {fullName}, tài khoản của bạn đã hoạt động bình thường trở lại.</p>
                    <div style='margin: 40px 0;'>
                        <a href='{_configuration["AppSettings:BaseUrl"]}/login' style='{GetButtonStyle(PrimaryColor)}'>Đăng nhập ngay</a>
                    </div>
                </div>";

            await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Trạng thái tài khoản", content));
        }

        public async Task<bool> SendRefundRequestInstructionAsync(string toEmail, string userName, string className, DateTime classStartDateTime, string? reason = null)
        {
            var subject = "Lớp học bị hủy & Hướng dẫn hoàn tiền";

            var content = $@"
                <div>
                    <h2 style='margin-top: 10px;'>Lớp học đã bị hủy</h2>
                    <p style='color: {TextLight};'>Chào {userName}, rất tiếc lớp học dưới đây không thể diễn ra theo kế hoạch:</p>
                    
                    <div style='background: #FFFAE6; padding: 25px; border-radius: 12px; margin: 30px 0; border: 1px solid #FFC400;'>
                        <div style='font-size: 20px; font-weight: 800; color: {TextDark}; margin-bottom: 5px;'>{className}</div>
                        <div style='color: {TextLight}; font-weight: 500;'>📅 {classStartDateTime:dd/MM/yyyy HH:mm}</div>
                        {(reason != null ? $"<div style='margin-top: 15px; padding-top: 15px; border-top: 1px dashed #E6B200; color: {TextDark}; font-weight: 500;'><strong>Lý do:</strong> {reason}</div>" : "")}
                    </div>

                    <div style='margin-top: 30px;'>
                        <h3 style='color: {PrimaryColor}; font-size: 18px; margin-bottom: 15px;'>Hướng dẫn hoàn tiền:</h3>
                        <ol style='color: {TextDark}; padding-left: 20px; line-height: 1.8; font-weight: 600;'>
                            <li>Đăng nhập vào Website Flearn.</li>
                            <li>Vào mục <strong>""Lớp học của tôi""</strong>.</li>
                            <li>Chọn lớp trên và nhấn nút <strong>""Yêu cầu hoàn tiền""</strong>.</li>
                        </ol>
                    </div>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Hoàn tiền", content));
        }

        public async Task<bool> SendPayoutRequestApprovedAsync(string toEmail, string teacherName, decimal amount, string bankName, string accountNumber, string? transactionRef = null, string? adminNote = null)
        {
            var subject = "💰 Tiền đã về tài khoản";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {SuccessColor}; margin-top: 20px;'>Giao dịch thành công</h2>
                    <p style='color: {TextLight}; margin-bottom: 30px;'>Yêu cầu rút tiền của <strong>{teacherName}</strong> đã hoàn tất.</p>

                    <div style='background-color: #FFFFFF; border: 2px solid #F4F5F7; border-radius: 16px; padding: 30px; margin: 20px 0; box-shadow: 0 8px 20px rgba(0,0,0,0.06); text-align: left;'>
                        <div style='display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #F4F5F7; padding-bottom: 20px; margin-bottom: 20px;'>
                            <span style='color: {TextLight}; font-weight: 700; text-transform: uppercase; letter-spacing: 1px; font-size: 12px;'>Số tiền nhận</span>
                            <span style='color: {SuccessColor}; font-weight: 800; font-size: 32px;'>{amount:N0} đ</span>
                        </div>
                        <div style='font-size: 16px; color: {TextDark}; line-height: 1.8;'>
                            <div style='margin-bottom: 8px;'><strong>Ngân hàng:</strong> {bankName}</div>
                            <div style='margin-bottom: 8px;'><strong>STK:</strong> {accountNumber}</div>
                            <div style='color: {TextLight}; font-size: 13px; margin-top: 15px; font-family: monospace;'>Mã GD: {transactionRef ?? "N/A"}</div>
                        </div>
                         {(!string.IsNullOrWhiteSpace(adminNote) ? $"<div style='margin-top: 20px; padding: 15px; background: #F4F5F7; border-radius: 8px; font-size: 14px; color: {TextDark}; font-weight: 500;'>💬 <strong>Ghi chú:</strong> {adminNote}</div>" : "")}
                    </div>
                    <p style='color: {TextLight}; font-size: 13px; margin-top: 25px;'>Vui lòng kiểm tra tài khoản ngân hàng của bạn.</p>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Thanh toán", content));
        }

        public async Task<bool> SendPayoutRequestRejectedAsync(string toEmail, string teacherName, decimal amount, string rejectionReason)
        {
            var subject = "Yêu cầu rút tiền không thành công";
            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {DangerColor}; margin-top: 20px;'>Giao dịch bị từ chối</h2>
                    <p style='color: {TextLight};'>Số tiền <strong>{amount:N0} đ</strong> đã được hoàn lại vào ví Flearn.</p>
                    
                    <div style='background: #FFF5F5; color: {TextDark}; padding: 20px; border-radius: 12px; margin: 30px auto; display: inline-block; width: 100%; box-sizing: border-box; text-align: left; border: 1px solid #FFBDAD;'>
                        <div style='color: {DangerColor}; font-weight: 700; font-size: 12px; text-transform: uppercase; margin-bottom: 8px;'>Lý do từ chối</div>
                        <div style='font-weight: 600; font-size: 16px;'>{rejectionReason}</div>
                    </div>
                    <p style='color: {TextLight}; font-size: 14px;'>Vui lòng kiểm tra lại thông tin và thử lại.</p>
                </div>";
            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Thanh toán", content));
        }

        public async Task<bool> SendRefundRequestConfirmationAsync(string toEmail, string userName, string className, string refundRequestId)
        {
            var subject = "Đã tiếp nhận yêu cầu hoàn tiền";
            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='margin-top: 20px;'>Yêu cầu đã được gửi</h2>
                    <p style='color: {TextLight};'>Chúng tôi đang xử lý yêu cầu hoàn tiền cho lớp <strong>{className}</strong>.</p>
                    
                    <div style='background: #F4F5F7; padding: 25px; border-radius: 12px; margin: 35px auto; display: inline-block; border: 2px solid #EBECF0;'>
                        <span style='display: block; font-size: 12px; color: {TextLight}; text-transform: uppercase; letter-spacing: 1px; font-weight: 800; margin-bottom: 10px;'>Mã tham chiếu</span>
                        <span style='display: block; font-size: 28px; color: {TextDark}; font-weight: 800; font-family: monospace;'>{refundRequestId}</span>
                    </div>
                    
                    <p style='color: {TextLight}; font-size: 14px;'>Thời gian xử lý: <strong>3-5 ngày làm việc</strong>.</p>
                </div>";
            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Hoàn tiền", content));
        }

        public async Task<bool> SendRefundRequestApprovedAsync(string toEmail, string userName, string className, decimal refundAmount, string? proofImageUrl = null, string? adminNote = null)
        {
            var subject = "Hoàn tiền thành công";

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {SuccessColor}; margin-top: 20px;'>Yêu cầu được chấp nhận</h2>
                    <p style='color: {TextLight};'>Chúng tôi đã hoàn tiền cho lớp <strong>{className}</strong>.</p>
                    
                    <div style='text-align: center; margin: 40px 0;'>
                        <div style='display: inline-block; padding: 20px 50px; border-radius: 60px; background-color: #E3FCEF; color: {SuccessColor}; font-weight: 800; font-size: 36px; box-shadow: 0 4px 15px rgba(54, 179, 126, 0.15);'>
                            +{refundAmount:N0} đ
                        </div>
                    </div>

                    {(!string.IsNullOrWhiteSpace(adminNote) ? $"<p style='background:#F4F5F7; padding:15px; border-radius:8px; color:{TextDark}; font-size:15px; margin: 20px 0;'>💬 <strong>Ghi chú:</strong> {adminNote}</p>" : "")}
                    
                    {(proofImageUrl != null ? $"<div style='margin-top: 30px;'><a href='{proofImageUrl}' style='color: {PrimaryColor}; font-weight: 700; text-decoration: none; border-bottom: 2px solid {PrimaryColor}; padding-bottom: 4px;'>📎 Xem biên lai chuyển khoản</a></div>" : "")}
                    
                    <p style='color: {TextLight}; font-size: 13px; text-align: center; margin-top: 30px;'>Tiền sẽ về tài khoản trong 1-3 ngày làm việc.</p>
                </div>";
            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Hoàn tiền", content));
        }

        public async Task<bool> SendRefundRequestRejectedAsync(string toEmail, string userName, string className, string rejectionReason)
        {
            var subject = "Từ chối hoàn tiền";
            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='color: {DangerColor}; margin-top: 20px;'>Yêu cầu bị từ chối</h2>
                    <p style='color: {TextLight}; margin-bottom: 30px;'>Yêu cầu hoàn tiền cho lớp <strong>{className}</strong> không đủ điều kiện.</p>
                    
                    <div style='background-color: #FFF5F5; padding: 25px; border-radius: 12px; margin: 0 auto; text-align: left; border: 1px solid #FFBDAD; width: 100%; box-sizing: border-box;'>
                        <div style='color: {DangerColor}; font-weight: 700; text-transform: uppercase; font-size: 12px; margin-bottom: 8px;'>Lý do</div>
                        <div style='color: {TextDark}; font-weight: 600; font-size: 16px;'>{rejectionReason}</div>
                    </div>
                    <p style='color: {TextLight}; font-size: 14px; margin-top: 30px;'>Vui lòng liên hệ bộ phận hỗ trợ nếu bạn cần giải thích thêm.</p>
                </div>";
            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate("Hoàn tiền", content));
        }

        // --- HELPERS ---

        private async Task<bool> SendOtpEmailBase(string toEmail, string userName, string otpCode, string title, bool isWarning = false)
        {
            var subject = $"{title} - Mã xác thực";
            var color = isWarning ? DangerColor : PrimaryColor;

            var content = $@"
                <div style='text-align: center;'>
                    <h2 style='margin-top: 20px;'>{title}</h2>
                    <p style='color: {TextLight}; font-size: 16px;'>Xin chào {userName}, mã xác thực của bạn là:</p>
                    
                    <div style='margin: 40px 0;'>
                        <span style='font-family: monospace; font-size: 42px; font-weight: 800; color: {color}; letter-spacing: 8px; background: #FFFFFF; padding: 25px 50px; border-radius: 16px; box-shadow: 0 4px 15px rgba(0,0,0,0.05); border: 2px solid {color}; display: inline-block;'>
                            {otpCode}
                        </span>
                    </div>
                    <p style='color: {TextLight}; font-size: 14px;'>Mã có hiệu lực trong 5 phút. Tuyệt đối không chia sẻ với ai.</p>
                </div>";

            return await SendEmailAsync(toEmail, subject, GetPremiumTemplate(title, content));
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var smtpClient = new SmtpClient(_configuration["EmailSettings:SmtpHost"])
                {
                    Port = int.Parse(_configuration["EmailSettings:SmtpPort"]),
                    Credentials = new NetworkCredential(
                        _configuration["EmailSettings:SmtpUser"],
                        _configuration["EmailSettings:Password"]
                    ),
                    EnableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"])
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_configuration["EmailSettings:FromEmail"], _configuration["EmailSettings:FromName"]),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);
                await smtpClient.SendMailAsync(mailMessage);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Email}", toEmail);
                return false;
            }
        }

        /// <summary>
        /// MASTER TEMPLATE
        /// </summary>
        /// <summary>
        /// MASTER TEMPLATE
        /// </summary>
        private string GetPremiumTemplate(string preheader, string innerHtml)
        {
            var year = DateTime.Now.Year;
            var appUrl = _configuration["AppSettings:BaseUrl"];
            var logoUrl = "https://res.cloudinary.com/dzo85c2kz/image/upload/v1764168994/logooo2_zanl1e.png";

        
            var uniqueId = Guid.NewGuid().ToString();
            var sentTime = TimeHelper.GetVietnamTime().ToString("dd/MM/yyyy HH:mm:ss");

            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Flearn Notification</title>
                <link href='https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700;800&display=swap' rel='stylesheet'>
                <style>
                    body {{ margin: 0; padding: 0; background-color: {BgBody}; font-family: 'Inter', Helvetica, Arial, sans-serif; color: {TextDark}; -webkit-font-smoothing: antialiased; }}
                    h1, h2, h3 {{ font-family: 'Inter', sans-serif; color: {TextDark}; font-weight: 800; letter-spacing: -0.8px; margin: 0; }}
                    h2 {{ font-size: 32px; line-height: 1.2; margin-bottom: 12px; }}
                    p {{ margin: 0 0 15px; line-height: 1.6; }}
                    .lead-text {{ font-size: 18px; color: {TextLight}; font-weight: 500; }}
                    .wrapper {{ width: 100%; table-layout: fixed; background-color: {BgBody}; padding: 40px 0; }}
                    .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 24px; overflow: hidden; box-shadow: 0 20px 40px rgba(0,0,0,0.06); }}
                    .header {{ padding: 30px 20px 10px; text-align: center; }}
                    .content {{ padding: 10px 50px 50px; }}
                    .footer {{ background-color: #FAFBFC; padding: 30px; text-align: center; font-size: 13px; color: {TextLight}; border-top: 1px solid #F0F2F5; }}
                    .footer a {{ color: {TextLight}; text-decoration: none; font-weight: 600; }}
                    .footer a:hover {{ color: {PrimaryColor}; }}
                    @media only screen and (max-width: 640px) {{
                        .wrapper {{ padding: 15px 10px; }}
                        .container {{ width: 100% !important; border-radius: 16px; }}
                        .header {{ padding: 25px 15px 0px; }}
                        .content {{ padding: 15px 25px 40px; }}
                        h2 {{ font-size: 24px !important; margin-bottom: 8px; }}
                        .logo-img {{ width: 180px !important; height: auto !important; }}
                    }}
                </style>
            </head>
            <body>
                <div class='wrapper'>
                    <div style='display:none;font-size:1px;color:{BgBody};line-height:1px;max-height:0px;max-width:0px;opacity:0;overflow:hidden;'>
                        {preheader}
                    </div>

                    <div class='container'>
                        <div class='header'>
                            <a href='{appUrl}'>
                                <img src='{logoUrl}' alt='Flearn' class='logo-img' 
                                     width='240' 
                                     style='display: block; margin: 0 auto; width: 240px; height: auto; border: 0;' />
                            </a>
                        </div>

                        <div class='content'>
                            {innerHtml}
                        </div>

                        <div class='footer'>
                            <p style='margin-bottom: 15px; font-weight: 500;'>&copy; {year} Flearn. Nền tảng học nói đa ngôn ngữ.</p>
                            <p style='margin-bottom: 0;'>
                                <a href='{appUrl}'>Trang chủ</a> • 
                                <a href='#'>Hỗ trợ</a>
                            </p>
                            
                            <div style='display:none; max-height:0px; overflow:hidden; mso-hide:all;'>
                                ID: {uniqueId} | Sent: {sentTime}
                            </div>
                            <div style='opacity: 0; font-size: 1px; color: #FAFBFC;'>
                                {Guid.NewGuid()} - Flearn Notification System
                            </div>
                        </div>
                    </div>
                </div>
            </body>
            </html>";
        }
        private string GetButtonStyle(string bgColor = PrimaryColor)
        {
            var background = bgColor == PrimaryColor
                ? $"background: linear-gradient(135deg, {PrimaryColor} 0%, {SecondaryColor} 100%);"
                : $"background-color: {bgColor};";

            return $@"
                display: inline-block; 
                {background}
                color: #ffffff; 
                padding: 18px 40px; 
                border-radius: 14px; 
                text-decoration: none; 
                font-weight: 700; 
                font-size: 16px; 
                letter-spacing: 0.5px;
                box-shadow: 0 8px 20px rgba(0, 82, 204, 0.2);
                border: none;
                text-align: center;
                transition: all 0.2s ease;";
        }
    }
}