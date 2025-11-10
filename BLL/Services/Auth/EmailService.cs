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

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<bool> SendWelcomeEmailAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "🎉 Chào mừng bạn đến với nền tảng học ngôn ngữ Flearn!";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Chào mừng đến Flearn</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea0%, #764ba2100%);'>
                        <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #667eea0%, #764ba2100%); padding:40px20px; text-align: center;'>
                                <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🎓 Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Nền tảng học ngôn ngữ thông minh</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding:40px30px;'>
                                <div style='text-align: center; margin-bottom:30px;'>
                                    <div style='background: linear-gradient(135deg, #667eea0%, #764ba2100%); width:80px; height:80px; border-radius:50%; margin:0 auto20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size:36px; color: white;'>🎉</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #f8f9ff; padding:25px; border-radius:12px; margin:25px0; border-left:4px solid #667eea;'>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0015px0;'>
                                        Cảm ơn bạn đã đăng ký tài khoản tại <strong>Flearn</strong> - nền tảng học ngôn ngữ hàng đầu!
                                    </p>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0;'>
                                        Chúng tôi rất vui mừng có bạn và sẵn sàng đồng hành cùng bạn trong hành trình chinh phục ngôn ngữ mới.
                                    </p>
                                </div>
                                
                                <div style='background: linear-gradient(135deg, #667eea0%, #764ba2100%); padding:25px; border-radius:12px; text-align: center; margin:30px0;'>
                                    <h3 style='color: white; margin:0015px0; font-size:20px;'>✅ Đăng ký thành công!</h3>
                                    <p style='color: rgba(255,255,255,0.9); margin:0; font-size:16px;'>Tài khoản của bạn đã được kích hoạt và sẵn sàng sử dụng</p>
                                </div>
                                
                                
                                <div style='background-color: #fff3cd; border:1px solid #ffeaa7; padding:20px; border-radius:8px; margin:25px0;'>
                                    <p style='margin:0; color: #856404; font-size:14px; text-align: center;'>
                                        💡 <strong>Mẹo:</strong> Hãy đăng nhập và khám phá các khóa học thú vị đang chờ bạn!
                                    </p>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #667eea; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin:0; font-size:12px;'>
                                    ©2025 Flearn - Nền tảng học ngôn ngữ thông minh<br/>
                                    📧 Bạn nhận được email này vì đã đăng ký tài khoản Flearn
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string toEmail, string userName, string resetToken)
        {
            try
            {
                var subject = "🔐 Yêu cầu đặt lại mật khẩu - Flearn";
                var resetLink = $"{_configuration["AppSettings:BaseUrl"]}/reset-password?token={resetToken}";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Đặt lại mật khẩu</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #ff6b6b0%, #ee5a24100%);'>
                        <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #ff6b6b0%, #ee5a24100%); padding:40px20px; text-align: center;'>
                                <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🔐 Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Đặt lại mật khẩu</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding:40px30px;'>
                                <div style='text-align: center; margin-bottom:30px;'>
                                    <div style='background: linear-gradient(135deg, #ff6b6b0%, #ee5a24100%); width:80px; height:80px; border-radius:50%; margin:0 auto20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size:36px; color: white;'>🔑</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #fff5f5; padding:25px; border-radius:12px; margin:25px0; border-left:4px solid #ff6b6b;'>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0015px0;'>
                                        Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                                    </p>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0;'>
                                        Nhấp vào nút bên dưới để tạo mật khẩu mới:
                                    </p>
                                </div>
                                
                                <div style='text-align: center; margin:30px0;'>
                                    <a href='{resetLink}' style='display: inline-block; background: linear-gradient(135deg, #ff6b6b0%, #ee5a24100%); color: white; padding:15px35px; text-decoration: none; border-radius:50px; font-weight:600; font-size:16px; box-shadow:04px15px rgba(255,107,107,0.4);'>
                                        🔐 Đặt lại mật khẩu
                                    </a>
                                </div>
                                
                                <div style='background-color: #fff3cd; border:1px solid #ffeaa7; padding:20px; border-radius:8px; margin:25px0;'>
                                    <p style='margin:0010px0; color: #856404; font-weight: bold; font-size:14px;'>⚠️ Lưu ý quan trọng:</p>
                                    <ul style='margin:0; padding-left:20px; color: #856404; font-size:14px;'>
                                        <li>Link này sẽ hết hạn sau <strong>24 giờ</strong></li>
                                        <li>Nếu không phải bạn yêu cầu, hãy bỏ qua email này</li>
                                        <li>Không chia sẻ link này với bất kỳ ai</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #ff6b6b; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin:0; font-size:12px;'>
                                    ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendEmailConfirmationAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "🔐 Mã OTP xác thực đăng ký - Flearn";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Mã OTP xác thực</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #3498db0%, #2980b9100%);'>
                        <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #3498db0%, #2980b9100%); padding:40px20px; text-align: center;'>
                                <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🛡️ Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Xác thực tài khoản</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding:40px30px;'>
                                <div style='text-align: center; margin-bottom:30px;'>
                                    <div style='background: linear-gradient(135deg, #3498db0%, #2980b9100%); width:80px; height:80px; border-radius:50%; margin:0 auto20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size:36px; color: white;'>📧</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #f0f8ff; padding:25px; border-radius:12px; margin:25px0; border-left:4px solid #3498db;'>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0015px0;'>
                                        Để hoàn tất đăng ký tài khoản, vui lòng sử dụng mã OTP bên dưới:
                                    </p>
                                </div>
                                
                                <!-- OTP Code -->
                                <div style='text-align: center; margin:40px0;'>
                                    <div style='background: linear-gradient(135deg, #3498db0%, #2980b9100%); color: white; font-size:36px; font-weight: bold; padding:25px20px; border-radius:15px; letter-spacing:12px; display: inline-block; box-shadow:08px25px rgba(52,152,219,0.3);'>
                                        {otpCode}
                                    </div>
                                    <p style='color: #7f8c8d; margin:15px000; font-size:14px;'>Mã xác thực OTP của bạn</p>
                                </div>
                                
                                <div style='background-color: #fff3cd; border:1px solid #ffeaa7; padding:20px; border-radius:8px; margin:25px0;'>
                                    <p style='margin:0010px0; color: #856404; font-weight: bold; font-size:14px;'>⚠️ Lưu ý quan trọng:</p>
                                    <ul style='margin:0; padding-left:20px; color: #856404; font-size:14px;'>
                                        <li>Mã này sẽ hết hạn sau <strong>5 phút</strong></li>
                                        <li>Vui lòng hoàn tất đăng ký trước khi mã hết hạn</li>
                                        <li>Không chia sẻ mã này với bất kỳ ai</li>
                                        <li>Nếu không phải bạn đăng ký, hãy bỏ qua email này</li>
                                    </ul>
                                </div>
                                
                                <div style='text-align: center; margin:30px0;'>
                                    <p style='color: #7f8c8d; font-size:14px; margin:0;'>
                                        Sau khi xác thực thành công, bạn sẽ có thể trải nghiệm đầy đủ các tính năng của Flearn! 🎉
                                    </p>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #3498db; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin:0; font-size:12px;'>
                                    ©2025 Flearn - Nền tảng học ngôn ngữ thông minh<br/>
                                    📧 Bạn nhận được email này vì đã yêu cầu đăng ký tài khoản Flearn
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to {Email}", toEmail);
                return false;
            }
        }

        // New: resend OTP
        public async Task<bool> SendOtpResendAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "🔁 Mã OTP - Flearn (Gửi lại)";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Mã OTP</title>
                    </head>
                    <body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f0f2f5;'>
                        <div style='max-width:600px;margin:0 auto;background:#ffffff;padding:30px;border-radius:8px;'>
                            <h2 style='color:#2c3e50;'>Xin chào {userName},</h2>
                            <p style='color:#333;'>Bạn đã yêu cầu gửi lại mã OTP. Mã OTP của bạn là:</p>
                            <div style='font-size:32px;font-weight:bold;background:#3498db;color:#fff;padding:16px;border-radius:8px;display:inline-block;margin:10px0;'>{otpCode}</div>
                            <p style='color:#6c757d;'>Mã sẽ hết hạn sau 5 phút. Nếu bạn không yêu cầu, vui lòng bỏ qua email này.</p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP resend email to {Email}", toEmail);
                return false;
            }
        }

        // New: password reset OTP
        public async Task<bool> SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "🔐 Mã OTP đặt lại mật khẩu - Flearn";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Mã OTP đặt lại mật khẩu</title>
                    </head>
                    <body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f8f9fa;'>
                        <div style='max-width:600px;margin:0 auto;background:#ffffff;padding:30px;border-radius:8px;'>
                            <h2 style='color:#2c3e50;'>Xin chào {userName},</h2>
                            <p style='color:#333;'>Bạn đã yêu cầu đặt lại mật khẩu. Vui lòng sử dụng mã OTP sau để xác nhận:</p>
                            <div style='font-size:32px;font-weight:bold;background:#ff6b6b;color:#fff;padding:16px;border-radius:8px;display:inline-block;margin:10px0;'>{otpCode}</div>
                            <p style='color:#6c757d;'>Mã sẽ hết hạn sau 5 phút. Nếu bạn không yêu cầu, hãy bỏ qua email này.</p>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset OTP to {Email}", toEmail);
                return false;
            }
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

        public async Task<bool> SendTeacherApplicationSubmittedAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "📝 Đơn ứng tuyển giáo viên đã được gửi - Flearn";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Đơn ứng tuyển đã được gửi</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #28a7450%, #20c997100%);'>
                    <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #28a7450%, #20c997100%); padding:40px20px; text-align: center;'>
                    <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🎓 Flearn</h1>
                    <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Ứng tuyển giáo viên</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding:40px30px;'>
                    <div style='text-align: center; margin-bottom:30px;'>
                    <div style='background: linear-gradient(135deg, #28a7450%, #20c997100%); width:80px; height:80px; border-radius:50%; margin:0 auto20px; display: flex; align-items: center; justify-content: center;'>
                    <span style='font-size:36px; color: white;'>📝</span>
                    </div>
                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Xin chào {userName}!</h2>
                    </div>
                    
                    <div style='background-color: #f0fff4; padding:25px; border-radius:12px; margin:25px0; border-left:4px solid #28a745;'>
                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0015px0;'>
                    Cảm ơn bạn đã gửi đơn ứng tuyển làm giáo viên tại <strong>Flearn</strong>!
                    </p>
                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0;'>
                    Đơn ứng tuyển của bạn đã được tiếp nhận và đang trong quá trình xem xét.
                    </p>
                    </div>
                    
                    <div style='background: linear-gradient(135deg, #28a7450%, #20c997100%); padding:25px; border-radius:12px; text-align: center; margin:30px0;'>
                    <h3 style='color: white; margin:0015px0; font-size:20px;'>✅ Đơn đã được gửi thành công!</h3>
                    <p style='color: rgba(255,255,255,0.9); margin:0; font-size:16px;'>Chúng tôi sẽ phản hồi trong vòng 3-5 ngày làm việc</p>
                    </div>
                    
                    <div style='background-color: #fff3cd; border:1px solid #ffeaa7; padding:20px; border-radius:8px; margin:25px0;'>
                    <p style='margin:0; color: #856404; font-size:14px; text-align: center;'>
                    💡 <strong>Lưu ý:</strong> Bạn có thể theo dõi trạng thái đơn ứng tuyển trong tài khoản của mình
                    </p>
                    </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                    <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                    <a href='mailto:support@flearn.com' style='color: #28a745; text-decoration: none;'>support@flearn.com</a>
                    </p>
                    <p style='color: #6c757d; margin:0; font-size:12px;'>
                    ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
                    </p>
                    </div>
                    </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending teacher application submitted email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTeacherApplicationApprovedAsync(string toEmail, string userName)
        {
            try
            {
                var subject = "🎉 Chúc mừng! Đơn ứng tuyển giáo viên đã được duyệt - Flearn";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                    <meta charset='UTF-8'>
                    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                    <title>Đơn ứng tuyển được duyệt</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #ffc1070%, #ff8800100%);'>
                    <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #ffc1070%, #ff8800100%); padding:40px20px; text-align: center;'>
                    <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🎓 Flearn</h1>
                    <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Chào mừng giáo viên mới!</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding:40px30px;'>
                    <div style='text-align: center; margin-bottom:30px;'>
                    <div style='background: linear-gradient(135deg, #ffc1070%, #ff8800100%); width:80px; height:80px; border-radius:50%; margin:0 auto20px; display: flex; align-items: center; justify-content: center;'>
                    <span style='font-size:36px; color: white;'>🎉</span>
                    </div>
                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Chúc mừng {userName}!</h2>
                    </div>
                    
                    <div style='background-color: #fff8e1; padding:25px; border-radius:12px; margin:25px0; border-left:4px solid #ffc107;'>
                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0015px0;'>
                    Đơn ứng tuyển làm giáo viên của bạn đã được <strong>phê duyệt</strong>!
                    </p>
                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0;'>
                    Chào mừng bạn gia nhập đội ngũ giáo viên Flearn. Bạn giờ đây có thể tạo và quản lý các khóa học của mình.
                    </p>
                    </div>
                    
                    <div style='background: linear-gradient(135deg, #ffc1070%, #ff8800100%); padding:25px; border-radius:12px; text-align: center; margin:30px0;'>
                    <h3 style='color: white; margin:0015px0; font-size:20px;'>🌟 Bạn giờ đây là giáo viên Flearn!</h3>
                    <p style='color: rgba(255,255,255,0.9); margin:0; font-size:16px;'>Hãy bắt đầu tạo khóa học đầu tiên của bạn</p>
                    </div>
                    
                    <div style='text-align: center; margin:30px0;'>
                    <a href='{_configuration["AppSettings:BaseUrl"]}/teacher/dashboard' style='display: inline-block; background: linear-gradient(135deg, #ffc1070%, #ff8800100%); color: white; padding:15px35px; text-decoration: none; border-radius:50px; font-weight:600; font-size:16px; box-shadow:04px15px rgba(255,193,7,0.4);'>
                    🚀 Bắt đầu giảng dạy
                    </a>
                    </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                    <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                    <a href='mailto:support@flearn.com' style='color: #ffc107; text-decoration: none;'>support@flearn.com</a>
                    </p>
                    <p style='color: #6c757d; margin:0; font-size:12px;'>
                    ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
                    </p>
                    </div>
                    </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending teacher application approved email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendTeacherApplicationRejectedAsync(string toEmail, string userName, string reason)
        {
            try
            {
                var subject = "📋 Thông báo về đơn ứng tuyển giáo viên - Flearn";
                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Thông báo đơn ứng tuyển</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #6c757d0%, #495057100%);'>
                        <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #6c757d0%, #495057100%); padding:40px20px; text-align: center;'>
                                <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🎓 Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Thông báo đơn ứng tuyển</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding:40px30px;'>
                                <div style='text-align: center; margin-bottom:30px;'>
                                    <div style='background: linear-gradient(135deg, #6c757d0%, #495057100%); width:80px; height:80px; border-radius:50%; margin:0 auto20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size:36px; color: white;'>📋</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #f8f9fa; padding:25px; border-radius:12px; margin:25px0; border-left:4px solid #6c757d;'>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0015px0;'>
                                        Cảm ơn bạn đã quan tâm và gửi đơn ứng tuyển làm giáo viên tại Flearn.
                                    </p>
                                    <p style='font-size:16px; line-height:1.6; color: #333; margin:0;'>
                                        Sau khi xem xét kỹ lưỡng, chúng tôi rất tiếc phải thông báo rằng đơn ứng tuyển của bạn chưa được chấp nhận lần này.
                                    </p>
                                </div>
                                
                                <div style='background-color: #f8d7da; padding:20px; border-radius:8px; margin:25px0; border-left:4px solid #dc3545;'>
                                    <p style='margin:0010px0; color: #721c24; font-weight: bold; font-size:14px;'>Lý do:</p>
                                    <p style='margin:0; color: #721c24; font-size:14px;'>{reason}</p>
                                </div>
                                
                                <div style='background-color: #d1ecf1; border:1px solid #b3d7df; padding:20px; border-radius:8px; margin:25px0;'>
                                    <p style='margin:0; color: #0c5460; font-size:14px; text-align: center;'>
                                        💡 <strong>Gợi ý:</strong> Bạn có thể cải thiện hồ sơ và ứng tuyển lại sau 30 ngày
                                    </p>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #6c757d; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin:0; font-size:12px;'>
                                    ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending teacher application rejected email to {Email}", toEmail);
                return false;
            }
        }

        // ================== REFUND REQUEST EMAILS ==================
        public async Task<bool> SendRefundRequestInstructionAsync(
            string toEmail,
            string userName,
            string className,
            DateTime classStartDateTime,
            string? reason = null)
        {
            try
            {
                var subject = "📋 Hướng dẫn yêu cầu hoàn tiền - Flearn";
                var reasonSection = !string.IsNullOrEmpty(reason)
                    ? $"<p style='font-size: 16px; line-height: 1.6; color: #856404; margin: 15px 0 0 0;'><strong>Lý do:</strong> {reason}</p>"
                    : "";

                var body = $@"
                    <!DOCTYPE html>
                    <html>
                    <head>
                        <meta charset='UTF-8'>
                        <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                        <title>Hướng dẫn yêu cầu hoàn tiền</title>
                    </head>
                    <body style='margin:0; padding:0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea0%, #764ba2100%);'>
                        <div style='max-width:600px; margin:0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #667eea0%, #764ba2100%); padding:40px20px; text-align: center;'>
                                <h1 style='color: white; margin:0; font-size:28px; font-weight:700;'>🎓 Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin:10px000; font-size:16px;'>Hướng dẫn yêu cầu hoàn tiền</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding:40px30px;'>
                                <div style='text-align: center; margin-bottom:30px;'>
                                    <h2 style='color: #2c3e50; margin:0; font-size:24px; font-weight:600;'>Xin chào {userName}!</h2>
                                    <p style='color: #6c757d; margin:0; font-size:14px;'>Thông báo về lớp học của bạn</p>
                                </div
                            
                                <div style='background-color: #fff3cd; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #ffc107;'>
                                    <h3 style='color: #856404; margin: 0 0 15px 0; font-size: 18px;'>⚠️ Thông báo về lớp học</h3>
                                    <p style='font-size: 16px; line-height: 1.6; color: #856404; margin: 0;'>
                                        Lớp học <strong>{className}</strong> dự kiến diễn ra vào <strong>{classStartDateTime:dd/MM/yyyy HH:mm}</strong> đã bị hủy.
                                    </p>
                                    {reasonSection}
                                </div>
                                
                                <div style='background-color: #e7f3ff; padding: 30px; border-radius: 12px; margin: 30px 0;'>
                                    <h3 style='color: #004085; margin: 0 0 20px 0; font-size: 20px; text-align: center;'>📋 Hướng dẫn yêu cầu hoàn tiền</h3>
                                
                                    <div style='margin-bottom: 20px;'>
                                        <div style='background-color: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                            <h4 style='color: #667eea; margin: 0 0 10px 0; font-size: 16px;'>
                                                <span style='display: inline-block; width: 30px; height: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 50%; text-align: center; line-height: 30px; margin-right: 10px;'>1</span>
                                                Đăng nhập vào tài khoản
                                            </h4>
                                            <p style='margin: 0 0 0 40px; color: #555; font-size: 14px;'>Truy cập vào phần ""Lớp học của tôi""</p>
                                        </div>
                                
                                        <div style='background-color: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                            <h4 style='color: #667eea; margin: 0 0 10px 0; font-size: 16px;'>
                                                <span style='display: inline-block; width: 30px; height: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 50%; text-align: center; line-height: 30px; margin-right: 10px;'>2</span>
                                                Chọn ""Gửi đơn yêu cầu hoàn tiền""
                                            </h4>
                                            <p style='margin: 0 0 0 40px; color: #555; font-size: 14px;'>Tìm lớp học: <strong>{className}</strong></p>
                                        </div>
                                
                                        <div style='background-color: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                            <h4 style='color: #667eea; margin: 0 0 10px 0; font-size: 16px;'>
                                                <span style='display: inline-block; width: 30px; height: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 50%; text-align: center; line-height: 30px; margin-right: 10px;'>3</span>
                                                Chọn loại yêu cầu hoàn tiền
                                            </h4>
                                            <p style='margin: 0 0 0 40px; color: #555; font-size: 14px;'>Ví dụ: ""Lớp học bị hủy"", ""Lý do cá nhân"", ""Khác""...</p>
                                        </div>
                                
                                        <div style='background-color: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                            <h4 style='color: #667eea; margin: 0 0 10px 0; font-size: 16px;'>
                                                <span style='display: inline-block; width: 30px; height: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 50%; text-align: center; line-height: 30px; margin-right: 10px;'>4</span>
                                                Nhập thông tin ngân hàng
                                            </h4>
                                            <ul style='margin: 5px 0 0 40px; color: #555; font-size: 14px; padding-left: 20px;'>
                                                <li>Tên ngân hàng</li>
                                                <li>Số tài khoản</li>
                                                <li>Tên chủ tài khoản</li>
                                            </ul>
                                        </div>
                                
                                        <div style='background-color: white; padding: 15px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                            <h4 style='color: #667eea; margin: 0 0 10px 0; font-size: 16px;'>
                                                <span style='display: inline-block; width: 30px; height: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 50%; text-align: center; line-height: 30px; margin-right: 10px;'>5</span>
                                                Gửi đơn
                                            </h4>
                                            <p style='margin: 0 0 0 40px; color: #555; font-size: 14px;'>Kiểm tra lại thông tin và nhấn ""Gửi yêu cầu"" để hoàn tất.</p>
                                        </div>
                                    </div>
                                
                                    <div style='text-align:center; margin-top:20px;'>
                                        <a href='{_configuration["AppSettings:BaseUrl"]}' style='display:inline-block; background: linear-gradient(135deg, #667eea0%, #764ba2100%); color:#fff; padding:12px24px; text-decoration:none; border-radius:8px; font-weight:600;'>Đăng nhập Flearn</a>
                                    </div>
                                </div>
                                
                                <div style='background-color: #f8f9fa; padding:30px; text-align: center; border-top:1px solid #e9ecef;'>
                                    <p style='color: #6c757d; margin:0010px0; font-size:14px;'>
                                        Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                        <a href='mailto:support@flearn.com' style='color: #667eea; text-decoration: none;'>support@flearn.com</a>
                                    </p>
                                    <p style='color: #6c757d; margin:0; font-size:12px;'>
                                        ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
                                    </p>
                                </div>
                            </div>
                        </div>
                    </body>
                    </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund instruction email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendRefundRequestConfirmationAsync(
            string toEmail,
            string userName,
            string className,
            string refundRequestId)
        {
            try
            {
                var subject = "✅ Xác nhận đã nhận yêu cầu hoàn tiền - Flearn";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
 <meta charset='UTF-8'>
 <meta name='viewport' content='width=device-width, initial-scale=1.0'>
 <title>Xác nhận yêu cầu hoàn tiền</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f0f2f5;'>
 <div style='max-width:600px;margin:0 auto;background:#ffffff;'>
 <div style='background:linear-gradient(135deg,#28a7450%,#20c997100%);padding:40px20px;text-align:center;'>
 <h1 style='color:#fff;margin:0;font-size:28px;font-weight:700;'>Flearn</h1>
 <p style='color:rgba(255,255,255,0.9);margin:10px000;font-size:16px;'>Xác nhận yêu cầu hoàn tiền</p>
 </div>
 <div style='padding:40px30px;'>
 <h2 style='color:#2c3e50;margin:0010px0;font-size:22px;font-weight:600;'>Xin chào {userName},</h2>
 <p style='color:#333;line-height:1.6;'>Chúng tôi đã nhận được yêu cầu hoàn tiền cho lớp học <strong>{className}</strong>.</p>
 <p style='color:#333;line-height:1.6;'>Mã yêu cầu của bạn là: <strong>{refundRequestId}</strong>. Vui lòng lưu lại để tiện tra cứu.</p>
 <div style='background:#f8f9fa;border:1px solid #e9ecef;border-radius:8px;padding:16px;margin-top:16px;'>
 <p style='margin:0;color:#6c757d;font-size:14px;'>Thời gian xử lý dự kiến:3-5 ngày làm việc.</p>
 </div>
 </div>
 <div style='background:#f8f9fa;padding:30px;text-align:center;border-top:1px solid #e9ecef;'>
 <p style='color:#6c757d;margin:0010px0;font-size:14px;'>
 Cần hỗ trợ? Liên hệ <a href='mailto:support@flearn.com' style='color:#20c997;text-decoration:none;'>support@flearn.com</a>
 </p>
 <p style='color:#6c757d;margin:0;font-size:12px;'>©2025 Flearn</p>
 </div>
 </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund confirmation email to {Email}", toEmail);
                return false;
            }
        }

        // Renamed: this method previously had incorrect name/signature. Now matches IEmailService.SendPayoutRequestApprovedAsync
        public async Task<bool> SendPayoutRequestApprovedAsync(
            string toEmail,
            string teacherName,
            decimal amount,
            string bankName,
            string accountNumber,
            string? transactionRef = null,
            string? adminNote = null)
        {
            try
            {
                var subject = "✅ Yêu cầu rút tiền đã được duyệt - Flearn";

                var transactionSection = !string.IsNullOrWhiteSpace(transactionRef)
                    ? $"<p style='margin:10px0;color:#333;line-height:1.6;'><strong>Mã giao dịch:</strong> <span style='color:#28a745;font-family:monospace;'>{transactionRef}</span></p>"
                    : string.Empty;

                var noteSection = !string.IsNullOrWhiteSpace(adminNote)
                    ? $"<div style='background:#e7f3ff;border-left:4px solid #007bff;padding:16px;border-radius:8px;margin:20px0;'><p style='margin:0;color:#004085;font-size:14px;'><strong>Ghi chú từ Admin:</strong> {adminNote}</p></div>"
                    : string.Empty;

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Rút tiền được duyệt</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:linear-gradient(135deg,#28a7450%,#20c997100%);'>
    <div style='max-width:600px;margin:0 auto;background:#ffffff;'>
        <div style='background:linear-gradient(135deg,#28a7450%,#20c997100%);padding:40px20px;text-align:center;'>
            <h1 style='color:#fff;margin:0;font-size:28px;font-weight:700;'>💰 Flearn</h1>
      <p style='color:rgba(255,255,255,0.95);margin:10px000;font-size:16px;'>Yêu cầu rút tiền đã được duyệt</p>
    </div>
        <div style='padding:40px30px;'>
     <div style='text-align:center;margin-bottom:30px;'>
       <div style='background:linear-gradient(135deg,#28a7450%,#20c997100%);width:80px;height:80px;border-radius:50%;margin:0 auto20px;display:flex;align-items:center;justify-content:center;'>
     <span style='font-size:40px;'>✅</span>
            </div>
       <h2 style='color:#2c3e50;margin:0;font-size:24px;font-weight:600;'>Chúc mừng {teacherName}!</h2>
     </div>
            <div style='background:#d4edda;border:1px solid #c3e6cb;padding:25px;border-radius:12px;margin:25px0;>
  <p style='margin:0010px0;color:#155724;font-size:16px;line-height:1.6;'>
              Yêu cầu rút tiền của bạn đã được <strong>duyệt thành công</strong>.
         </p>
       <p style='margin:0;color:#155724;font-size:16px;line-height:1.6;'>
Tiền sẽ được chuyển vào tài khoản ngân hàng của bạn trong vòng <strong>1-3 ngày làm việc</strong>.
   </p>
            </div>
            <div style='background:#f8f9fa;padding:25px;border-radius:12px;margin:25px0;border:1px solid #dee2e6;'>
        <h3 style='margin:0015px0;color:#495057;font-size:18px;'>📋 Thông tin giao dịch</h3>
    <div style='border-top:1px solid #dee2e6;padding-top:15px;'>
            <p style='margin:10px0;color:#333;line-height:1.6;'><strong>Số tiền:</strong> <span style='color:#28a745;font-size:20px;font-weight:bold;'>{amount:N0} VNĐ</span></p>
       <p style='margin:10px0;color:#333;line-height:1.6;'><strong>Ngân hàng:</strong> {bankName}</p>
            <p style='margin:10px0;color:#333;line-height:1.6;'><strong>Số tài khoản:</strong> {accountNumber}</p>
     {transactionSection}
     <p style='margin:10px00;color:#6c757d;font-size:14px;'><strong>Thời gian duyệt:</strong> {TimeHelper.GetVietnamTime():dd/MM/yyyy HH:mm}</p>
      </div>
            </div>
   {noteSection}
   <div style='background:#fff3cd;border:1px solid #ffeeba;padding:20px;border-radius:8px;margin:25px0;'>
      <p style='margin:0010px0;color:#856404;font-weight:bold;font-size:14px;'>💡 Lưu ý quan trọng:</p>
   <ul style='margin:0;padding-left:20px;color:#856404;font-size:14px;line-height:1.6;'>
      <li>Vui lòng kiểm tra tài khoản ngân hàng trong vòng 1-3 ngày làm việc</li>
 <li>Nếu quá thời gian trên chưa nhận được tiền, vui lòng liên hệ support</li>
         <li>Lưu lại email này để tra cứu giao dịch</li>
  </ul>
            </div>
   </div>
        <div style='background:#f8f9fa;padding:30px;text-align:center;border-top:1px solid #e9ecef;'>
      <p style='color:#6c757d;margin:0010px0;font-size:14px;'>
       Cần hỗ trợ? Liên hệ 
        <a href='mailto:support@flearn.com' style='color:#28a745;text-decoration:none;'>support@flearn.com</a>
  </p>
   <p style='color:#6c757d;margin:0;font-size:12px;'>
           ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
            </p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payout approved email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPayoutRequestRejectedAsync(
   string toEmail,
    string teacherName,
         decimal amount,
            string rejectionReason)
        {
            try
            {
                var subject = "❌ Yêu cầu rút tiền bị từ chối - Flearn";

                var body = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Rút tiền bị từ chối</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:linear-gradient(135deg,#dc35450%,#c82333100%);'>
    <div style='max-width:600px;margin:0 auto;background:#ffffff;padding:30px;border-radius:8px;'>
    <h2 style='color:#2c3e50;'>Xin chào {teacherName}!</h2>
    <p style='color:#333;'>Rất tiếc, yêu cầu rút tiền của bạn đã bị <strong>từ chối</strong>.</p>
    <p style='color:#333;'><strong>Số tiền hoàn:</strong> <span style='color:#dc3545;font-weight:bold;'>{amount:N0} VNĐ</span></p>
    
    <div style='background:#fff3cd;padding:15px;border-radius:8px;margin:15px0;border-left:4px solid #ffc107;'>
       <p style='margin:0;color:#856404;'><strong>Lý do:</strong> {rejectionReason}</p>
    </div>
    
    <div style='background:#d1ecf1;border:1px solid #bee5eb;padding:20px;border-radius:8px;margin:25px0;'>
      <p style='margin:0010px0;color:#0c5460;font-weight:bold;font-size:14px;'>💰 Thông tin ví:</p>
      <p style='margin:0;color:#0c5460;font-size:14px;line-height:1.6;'>
        Số tiền <strong>{amount:N0} VNĐ</strong> đã được cộng trở lại vào số dư khả dụng của bạn. 
     Bạn có thể tạo yêu cầu rút tiền mới sau khi khắc phục các vấn đề.
     </p>
         </div>
         <div style='background:#e7f3ff;border:1px solid #b8daff;padding:20px;border-radius:8px;margin:25px0;'>
    <p style='margin:0010px0;color:#004085;font-weight:bold;font-size:14px;'>📌 Hành động tiếp theo:</p>
         <ul style='margin:0;padding-left:20px;color:#004085;font-size:14px;line-height:1.6;'>
             <li>Kiểm tra và cập nhật thông tin tài khoản ngân hàng nếu cần</li>
     <li>Đảm bảo đáp ứng các điều kiện rút tiền</li>
         <li>Liên hệ support nếu cần hỗ trợ thêm</li>
     <li>Có thể tạo yêu cầu rút tiền mới sau khi khắc phục</li>
        </ul>
            </div>
        <div style='background:#f8f9fa;padding:30px;text-align:center;border-top:1px solid #e9ecef;'>
  <p style='color:#6c757d;margin:0010px0;font-size:14px;'>
  Cần hỗ trợ? Liên hệ 
        <a href='mailto:support@flearn.com' style='color:#dc3545;text-decoration:none;'>support@flearn.com</a>
 </p>
   <p style='color:#6c757d;margin:0;font-size:12px;'>
        ©2025 Flearn - Nền tảng học ngôn ngữ thông minh
     </p>
        </div>
    </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payout rejected email to {Email}", toEmail);
                return false;
            }
        }


        public async Task<bool> SendRefundRequestApprovedAsync(
            string toEmail,
            string userName,
            string className,
            decimal refundAmount,
            string? proofImageUrl = null,
            string? adminNote = null)
        {
            try
            {
                var subject = "✅ Yêu cầu hoàn tiền đã được chấp nhận - Flearn";

                var proofSection = !string.IsNullOrWhiteSpace(proofImageUrl)
                    ? $"<p style='margin:10px0;'><strong>Chứng từ:</strong> <a href='{proofImageUrl}' target='_blank'>Xem</a></p>"
                    : string.Empty;

                var noteSection = !string.IsNullOrWhiteSpace(adminNote)
                    ? $"<div style='background:#e7f3ff;border-left:4px solid #007bff;padding:16px;border-radius:8px;margin:20px0;'><p style='margin:0;color:#004085;font-size:14px;'><strong>Ghi chú từ Admin:</strong> {adminNote}</p></div>"
                    : string.Empty;

                var body = $@"
<!DOCTYPE html>
<html>
<head>
 <meta charset='UTF-8'>
 <meta name='viewport' content='width=device-width, initial-scale=1.0'>
 <title>Hoàn tiền được chấp nhận</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#f0f2f5;'>
 <div style='max-width:600px;margin:0 auto;background:#ffffff;padding:30px;border-radius:8px;'>
 <h2 style='color:#2c3e50;'>Xin chào {userName},</h2>
 <p style='color:#333;'>Yêu cầu hoàn tiền cho lớp <strong>{className}</strong> đã được <strong>chấp nhận</strong>.</p>
 <p style='color:#333;'><strong>Số tiền hoàn:</strong> <span style='color:#28a745;font-weight:bold;'>{refundAmount:N0} VNĐ</span></p>
 {proofSection}
 {noteSection}
 <p style='color:#6c757d;'>Số tiền sẽ được hoàn vào tài khoản đã đăng ký trong vòng 3-5 ngày làm việc.</p>
 </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund approved email to {Email}", toEmail);
                return false;
            }
        }


        public async Task<bool> SendRefundRequestRejectedAsync(
            string toEmail,
            string userName,
            string className,
            string rejectionReason)
        {
            try
            {
                var subject = "❌ Yêu cầu hoàn tiền bị từ chối - Flearn";
                var body = $@"
<!DOCTYPE html>
<html>
<head>
 <meta charset='UTF-8'>
 <meta name='viewport' content='width=device-width, initial-scale=1.0'>
 <title>Hoàn tiền bị từ chối</title>
</head>
<body style='margin:0;padding:0;font-family:Arial,sans-serif;background:#fff3f3;'>
 <div style='max-width:600px;margin:0 auto;background:#ffffff;padding:30px;border-radius:8px;'>
 <h2 style='color:#2c3e50;'>Xin chào {userName},</h2>
 <p style='color:#333;'>Rất tiếc, yêu cầu hoàn tiền cho lớp <strong>{className}</strong> đã bị <strong>từ chối</strong>.</p>
 <div style='background:#fff3cd;padding:15px;border-radius:8px;margin:15px0;border-left:4px solid #ffc107;'>
 <p style='margin:0;color:#856404;'><strong>Lý do:</strong> {rejectionReason}</p>
 </div>
 <p style='color:#6c757d;'>Nếu bạn cần hỗ trợ thêm, vui lòng liên hệ support.</p>
 </div>
</body>
</html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund rejected email to {Email}", toEmail);
                return false;
            }
        }
    }
}
