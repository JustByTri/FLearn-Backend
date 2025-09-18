using BLL.IServices.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
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
                    <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;'>
                                <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🎓 Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Nền tảng học ngôn ngữ thông minh</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding: 40px 30px;'>
                                <div style='text-align: center; margin-bottom: 30px;'>
                                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size: 36px; color: white;'>🎉</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #f8f9ff; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #667eea;'>
                                    <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                        Cảm ơn bạn đã đăng ký tài khoản tại <strong>Flearn</strong> - nền tảng học ngôn ngữ hàng đầu!
                                    </p>
                                    <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
                                        Chúng tôi rất vui mừng có bạn và sẵn sàng đồng hành cùng bạn trong hành trình chinh phục ngôn ngữ mới.
                                    </p>
                                </div>
                                
                                <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 25px; border-radius: 12px; text-align: center; margin: 30px 0;'>
                                    <h3 style='color: white; margin: 0 0 15px 0; font-size: 20px;'>✅ Đăng ký thành công!</h3>
                                    <p style='color: rgba(255,255,255,0.9); margin: 0; font-size: 16px;'>Tài khoản của bạn đã được kích hoạt và sẵn sàng sử dụng</p>
                                </div>
                                
                                
                                <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                                    <p style='margin: 0; color: #856404; font-size: 14px; text-align: center;'>
                                        💡 <strong>Mẹo:</strong> Hãy đăng nhập và khám phá các khóa học thú vị đang chờ bạn!
                                    </p>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #667eea; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin: 0; font-size: 12px;'>
                                    © 2024 Flearn - Nền tảng học ngôn ngữ thông minh<br/>
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
                    <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%);'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); padding: 40px 20px; text-align: center;'>
                                <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🔐 Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Đặt lại mật khẩu</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding: 40px 30px;'>
                                <div style='text-align: center; margin-bottom: 30px;'>
                                    <div style='background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size: 36px; color: white;'>🔑</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #fff5f5; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #ff6b6b;'>
                                    <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                        Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                                    </p>
                                    <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
                                        Nhấp vào nút bên dưới để tạo mật khẩu mới:
                                    </p>
                                </div>
                                
                                <div style='text-align: center; margin: 30px 0;'>
                                    <a href='{resetLink}' style='display: inline-block; background: linear-gradient(135deg, #ff6b6b 0%, #ee5a24 100%); color: white; padding: 15px 35px; text-decoration: none; border-radius: 50px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 15px rgba(255, 107, 107, 0.4);'>
                                        🔐 Đặt lại mật khẩu
                                    </a>
                                </div>
                                
                                <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                                    <p style='margin: 0 0 10px 0; color: #856404; font-weight: bold; font-size: 14px;'>⚠️ Lưu ý quan trọng:</p>
                                    <ul style='margin: 0; padding-left: 20px; color: #856404; font-size: 14px;'>
                                        <li>Link này sẽ hết hạn sau <strong>24 giờ</strong></li>
                                        <li>Nếu không phải bạn yêu cầu, hãy bỏ qua email này</li>
                                        <li>Không chia sẻ link này với bất kỳ ai</li>
                                    </ul>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #ff6b6b; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin: 0; font-size: 12px;'>
                                    © 2024 Flearn - Nền tảng học ngôn ngữ thông minh
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
                    <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #3498db 0%, #2980b9 100%);'>
                        <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                            <!-- Header -->
                            <div style='background: linear-gradient(135deg, #3498db 0%, #2980b9 100%); padding: 40px 20px; text-align: center;'>
                                <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🛡️ Flearn</h1>
                                <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Xác thực tài khoản</p>
                            </div>
                            
                            <!-- Content -->
                            <div style='padding: 40px 30px;'>
                                <div style='text-align: center; margin-bottom: 30px;'>
                                    <div style='background: linear-gradient(135deg, #3498db 0%, #2980b9 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                        <span style='font-size: 36px; color: white;'>📧</span>
                                    </div>
                                    <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                                </div>
                                
                                <div style='background-color: #f0f8ff; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #3498db;'>
                                    <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                        Để hoàn tất đăng ký tài khoản, vui lòng sử dụng mã OTP bên dưới:
                                    </p>
                                </div>
                                
                                <!-- OTP Code -->
                                <div style='text-align: center; margin: 40px 0;'>
                                    <div style='background: linear-gradient(135deg, #3498db 0%, #2980b9 100%); color: white; font-size: 36px; font-weight: bold; padding: 25px 20px; border-radius: 15px; letter-spacing: 12px; display: inline-block; box-shadow: 0 8px 25px rgba(52, 152, 219, 0.3);'>
                                        {otpCode}
                                    </div>
                                    <p style='color: #7f8c8d; margin: 15px 0 0 0; font-size: 14px;'>Mã xác thực OTP của bạn</p>
                                </div>
                                
                                <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                                    <p style='margin: 0 0 10px 0; color: #856404; font-weight: bold; font-size: 14px;'>⚠️ Lưu ý quan trọng:</p>
                                    <ul style='margin: 0; padding-left: 20px; color: #856404; font-size: 14px;'>
                                        <li>Mã này sẽ hết hạn sau <strong>5 phút</strong></li>
                                        <li>Vui lòng hoàn tất đăng ký trước khi mã hết hạn</li>
                                        <li>Không chia sẻ mã này với bất kỳ ai</li>
                                        <li>Nếu không phải bạn đăng ký, hãy bỏ qua email này</li>
                                    </ul>
                                </div>
                                
                                <div style='text-align: center; margin: 30px 0;'>
                                    <p style='color: #7f8c8d; font-size: 14px; margin: 0;'>
                                        Sau khi xác thực thành công, bạn sẽ có thể trải nghiệm đầy đủ các tính năng của Flearn! 🎉
                                    </p>
                                </div>
                            </div>
                            
                            <!-- Footer -->
                            <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                                <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                                    Cần hỗ trợ? Liên hệ với chúng tôi tại 
                                    <a href='mailto:support@flearn.com' style='color: #3498db; text-decoration: none;'>support@flearn.com</a>
                                </p>
                                <p style='color: #6c757d; margin: 0; font-size: 12px;'>
                                    © 2024 Flearn - Nền tảng học ngôn ngữ thông minh<br/>
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
    }
}

