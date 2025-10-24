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
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #28a745 0%, #20c997 100%);'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🎓 Flearn</h1>
                        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Ứng tuyển giáo viên</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 40px 30px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                <span style='font-size: 36px; color: white;'>📝</span>
                            </div>
                            <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                        </div>
                        
                        <div style='background-color: #f0fff4; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #28a745;'>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                Cảm ơn bạn đã gửi đơn ứng tuyển làm giáo viên tại <strong>Flearn</strong>!
                            </p>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
                                Đơn ứng tuyển của bạn đã được tiếp nhận và đang trong quá trình xem xét.
                            </p>
                        </div>
                        
                        <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 25px; border-radius: 12px; text-align: center; margin: 30px 0;'>
                            <h3 style='color: white; margin: 0 0 15px 0; font-size: 20px;'>✅ Đơn đã được gửi thành công!</h3>
                            <p style='color: rgba(255,255,255,0.9); margin: 0; font-size: 16px;'>Chúng tôi sẽ phản hồi trong vòng 3-5 ngày làm việc</p>
                        </div>
                        
                        <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                            <p style='margin: 0; color: #856404; font-size: 14px; text-align: center;'>
                                💡 <strong>Lưu ý:</strong> Bạn có thể theo dõi trạng thái đơn ứng tuyển trong tài khoản của mình
                            </p>
                        </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                        <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                            Cần hỗ trợ? Liên hệ với chúng tôi tại 
                            <a href='mailto:support@flearn.com' style='color: #28a745; text-decoration: none;'>support@flearn.com</a>
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
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #ffc107 0%, #ff8800 100%);'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #ffc107 0%, #ff8800 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🎓 Flearn</h1>
                        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Chào mừng giáo viên mới!</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 40px 30px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <div style='background: linear-gradient(135deg, #ffc107 0%, #ff8800 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                <span style='font-size: 36px; color: white;'>🎉</span>
                            </div>
                            <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Chúc mừng {userName}!</h2>
                        </div>
                        
                        <div style='background-color: #fff8e1; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #ffc107;'>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                Đơn ứng tuyển làm giáo viên của bạn đã được <strong>phê duyệt</strong>!
                            </p>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
                                Chào mừng bạn gia nhập đội ngũ giáo viên Flearn. Bạn giờ đây có thể tạo và quản lý các khóa học của mình.
                            </p>
                        </div>
                        
                        <div style='background: linear-gradient(135deg, #ffc107 0%, #ff8800 100%); padding: 25px; border-radius: 12px; text-align: center; margin: 30px 0;'>
                            <h3 style='color: white; margin: 0 0 15px 0; font-size: 20px;'>🌟 Bạn giờ đây là giáo viên Flearn!</h3>
                            <p style='color: rgba(255,255,255,0.9); margin: 0; font-size: 16px;'>Hãy bắt đầu tạo khóa học đầu tiên của bạn</p>
                        </div>
                        
                        <div style='text-align: center; margin: 30px 0;'>
                            <a href='{_configuration["AppSettings:BaseUrl"]}/teacher/dashboard' style='display: inline-block; background: linear-gradient(135deg, #ffc107 0%, #ff8800 100%); color: white; padding: 15px 35px; text-decoration: none; border-radius: 50px; font-weight: 600; font-size: 16px; box-shadow: 0 4px 15px rgba(255, 193, 7, 0.4);'>
                            🚀 Bắt đầu giảng dạy
                            </a>
                        </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                        <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                            Cần hỗ trợ? Liên hệ với chúng tôi tại 
                            <a href='mailto:support@flearn.com' style='color: #ffc107; text-decoration: none;'>support@flearn.com</a>
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
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #6c757d 0%, #495057 100%);'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #6c757d 0%, #495057 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🎓 Flearn</h1>
                        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Thông báo đơn ứng tuyển</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 40px 30px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <div style='background: linear-gradient(135deg, #6c757d 0%, #495057 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                <span style='font-size: 36px; color: white;'>📋</span>
                            </div>
                            <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                        </div>
                        
                        <div style='background-color: #f8f9fa; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #6c757d;'>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                Cảm ơn bạn đã quan tâm và gửi đơn ứng tuyển làm giáo viên tại Flearn.
                            </p>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
                                Sau khi xem xét kỹ lưỡng, chúng tôi rất tiếc phải thông báo rằng đơn ứng tuyển của bạn chưa được chấp nhận lần này.
                            </p>
                        </div>
                        
                        <div style='background-color: #f8d7da; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #dc3545;'>
                            <p style='margin: 0 0 10px 0; color: #721c24; font-weight: bold; font-size: 14px;'>Lý do:</p>
                            <p style='margin: 0; color: #721c24; font-size: 14px;'>{reason}</p>
                        </div>
                        
                        <div style='background-color: #d1ecf1; border: 1px solid #b3d7df; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                            <p style='margin: 0; color: #0c5460; font-size: 14px; text-align: center;'>
                                💡 <strong>Gợi ý:</strong> Bạn có thể cải thiện hồ sơ và ứng tuyển lại sau 30 ngày
                            </p>
                        </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                        <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                            Cần hỗ trợ? Liên hệ với chúng tôi tại 
                            <a href='mailto:support@flearn.com' style='color: #6c757d; text-decoration: none;'>support@flearn.com</a>
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
                _logger.LogError(ex, "Error sending teacher application rejected email to {Email}", toEmail);
                return false;
            }
        }
        public async Task<bool> SendOtpResendAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "🔁 Mã OTP mới - Flearn";
                var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Mã OTP mới</title>
            </head>
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #17a2b8 0%, #138496 100%);'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🔁 Flearn</h1>
                        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Mã OTP mới</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 40px 30px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <div style='background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                <span style='font-size: 36px; color: white;'>📱</span>
                            </div>
                            <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                        </div>
                        
                        <div style='background-color: #e7f6ff; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #17a2b8;'>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                Bạn đã yêu cầu gửi lại mã OTP. Đây là mã OTP mới của bạn:
                            </p>
                        </div>
                        
                        <!-- OTP Code -->
                        <div style='text-align: center; margin: 40px 0;'>
                            <div style='background: linear-gradient(135deg, #17a2b8 0%, #138496 100%); color: white; font-size: 36px; font-weight: bold; padding: 25px 20px; border-radius: 15px; letter-spacing: 12px; display: inline-block; box-shadow: 0 8px 25px rgba(23, 162, 184, 0.3);'>
                                {otpCode}
                            </div>
                            <p style='color: #7f8c8d; margin: 15px 0 0 0; font-size: 14px;'>Mã OTP mới của bạn</p>
                        </div>
                        
                        <div style='background-color: #fff3cd; border: 1px solid #ffeaa7; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                            <p style='margin: 0 0 10px 0; color: #856404; font-weight: bold; font-size: 14px;'>⚠️ Lưu ý quan trọng:</p>
                            <ul style='margin: 0; padding-left: 20px; color: #856404; font-size: 14px;'>
                                <li>Mã này sẽ hết hạn sau <strong>5 phút</strong></li>
                                <li>Mã OTP cũ đã không còn hiệu lực</li>
                                <li>Không chia sẻ mã này với bất kỳ ai</li>
                            </ul>
                        </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                        <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                            Cần hỗ trợ? Liên hệ với chúng tôi tại 
                            <a href='mailto:support@flearn.com' style='color: #17a2b8; text-decoration: none;'>support@flearn.com</a>
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
                _logger.LogError(ex, "Error sending resend OTP email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendPasswordResetOtpAsync(string toEmail, string userName, string otpCode)
        {
            try
            {
                var subject = "🔑 Mã OTP đặt lại mật khẩu - Flearn";
                var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Mã OTP đặt lại mật khẩu</title>
            </head>
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #fd7e14 0%, #e55a00 100%);'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #fd7e14 0%, #e55a00 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🔑 Flearn</h1>
                        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Đặt lại mật khẩu</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 40px 30px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <div style='background: linear-gradient(135deg, #fd7e14 0%, #e55a00 100%); width: 80px; height: 80px; border-radius: 50%; margin: 0 auto 20px; display: flex; align-items: center; justify-content: center;'>
                                <span style='font-size: 36px; color: white;'>🔐</span>
                            </div>
                            <h2 style='color: #2c3e50; margin: 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                        </div>
                        
                        <div style='background-color: #fff5f0; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #fd7e14;'>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0 0 15px 0;'>
                                Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn.
                            </p>
                            <p style='font-size: 16px; line-height: 1.6; color: #333; margin: 0;'>
                                Sử dụng mã OTP bên dưới để xác thực và đặt lại mật khẩu:
                            </p>
                        </div>
                        
                        <!-- OTP Code -->
                        <div style='text-align: center; margin: 40px 0;'>
                            <div style='background: linear-gradient(135deg, #fd7e14 0%, #e55a00 100%); color: white; font-size: 36px; font-weight: bold; padding: 25px 20px; border-radius: 15px; letter-spacing: 12px; display: inline-block; box-shadow: 0 8px 25px rgba(253, 126, 20, 0.3);'>
                                {otpCode}
                            </div>
                            <p style='color: #7f8c8d; margin: 15px 0 0 0; font-size: 14px;'>Mã OTP đặt lại mật khẩu</p>
                        </div>
                        
                        <div style='background-color: #f8d7da; border: 1px solid #f5c6cb; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                            <p style='margin: 0 0 10px 0; color: #721c24; font-weight: bold; font-size: 14px;'>🔒 Bảo mật quan trọng:</p>
                            <ul style='margin: 0; padding-left: 20px; color: #721c24; font-size: 14px;'>
                                <li>Mã này sẽ hết hạn sau <strong>10 phút</strong></li>
                                <li>Chỉ sử dụng mã này nếu bạn đã yêu cầu đặt lại mật khẩu</li>
                                <li>Không chia sẻ mã này với bất kỳ ai</li>
                                <li>Nếu không phải bạn yêu cầu, hãy bỏ qua email này</li>
                            </ul>
                        </div>
                    </div>
                    
                    <!-- Footer -->
                    <div style='background-color: #f8f9fa; padding: 30px; text-align: center; border-top: 1px solid #e9ecef;'>
                        <p style='color: #6c757d; margin: 0 0 10px 0; font-size: 14px;'>
                            Cần hỗ trợ? Liên hệ với chúng tôi tại 
                            <a href='mailto:support@flearn.com' style='color: #fd7e14; text-decoration: none;'>support@flearn.com</a>
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
                _logger.LogError(ex, "Error sending password reset OTP email to {Email}", toEmail);
                return false;
            }
        }

        public async Task<bool> SendRefundRequestInstructionAsync(
    string toEmail,
    string userName,
    string className,
    DateTime classStartDateTime)
        {
            try
            {
                var subject = "📋 Hướng dẫn yêu cầu hoàn tiền - Flearn";
                var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
                <meta name='viewport' content='width=device-width, initial-scale=1.0'>
                <title>Hướng dẫn yêu cầu hoàn tiền</title>
            </head>
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <!-- Header -->
                    <div style='background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px; font-weight: 700;'>🎓 Flearn</h1>
                        <p style='color: rgba(255,255,255,0.9); margin: 10px 0 0 0; font-size: 16px;'>Hướng dẫn yêu cầu hoàn tiền</p>
                    </div>
                    
                    <!-- Content -->
                    <div style='padding: 40px 30px;'>
                        <div style='text-align: center; margin-bottom: 30px;'>
                            <h2 style='color: #2c3e50; margin: 0 0 10px 0; font-size: 24px; font-weight: 600;'>Xin chào {userName}!</h2>
                            <p style='color: #6c757d; margin: 0; font-size: 14px;'>Thông báo về lớp học của bạn</p>
                        </div>
                        
                        <div style='background-color: #fff3cd; padding: 25px; border-radius: 12px; margin: 25px 0; border-left: 4px solid #ffc107;'>
                            <h3 style='color: #856404; margin: 0 0 15px 0; font-size: 18px;'>⚠️ Lớp học bị hủy</h3>
                            <p style='font-size: 16px; line-height: 1.6; color: #856404; margin: 0;'>
                                Rất tiếc, lớp học <strong>{className}</strong> dự kiến diễn ra vào <strong>{classStartDateTime:dd/MM/yyyy HH:mm}</strong> đã bị hủy do không đủ số lượng học viên tối thiểu.
                            </p>
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
                                    <p style='margin: 5px 0 0 40px; color: #555; font-size: 14px;'>Mã lớp: <strong>{classStartDateTime:yyyyMMddHHmm}</strong></p>
                                </div>
                                
                                <div style='background-color: white; padding: 15px; border-radius: 8px; margin-bottom: 15px; box-shadow: 0 2px 4px rgba(0,0,0,0.1);'>
                                    <h4 style='color: #667eea; margin: 0 0 10px 0; font-size: 16px;'>
                                        <span style='display: inline-block; width: 30px; height: 30px; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; border-radius: 50%; text-align: center; line-height: 30px; margin-right: 10px;'>3</span>
                                        Chọn loại yêu cầu
                                    </h4>
                                    <p style='margin: 0 0 0 40px; color: #555; font-size: 14px;'>Chọn: <strong>""Lớp học bị hủy - Không đủ học viên""</strong></p>
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
                                        Đính kèm hình ảnh (nếu có)
                                    </h4>
                                    <p style='margin: 0 0 0 40px; color: #555; font-size: 14px;'>Upload hình ảnh chứng minh thanh toán hoặc thông tin liên quan</p>
                                </div>
                            </div>
                        </div>
                        
                        <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 25px; border-radius: 12px; text-align: center; margin: 30px 0;'>
                            <h3 style='color: white; margin: 0 0 10px 0; font-size: 18px;'>⏱️ Thời gian xử lý</h3>
                            <p style='color: rgba(255,255,255,0.9); margin: 0; font-size: 16px;'>Yêu cầu của bạn sẽ được xử lý trong vòng <strong>3-5 ngày làm việc</strong></p>
                        </div>
                        
                        <div style='background-color: #d1ecf1; border: 1px solid #bee5eb; padding: 20px; border-radius: 8px; margin: 25px 0;'>
                            <p style='margin: 0; color: #0c5460; font-size: 14px; text-align: center;'>
                                💡 <strong>Lưu ý:</strong> Vui lòng kiểm tra kỹ thông tin ngân hàng trước khi gửi
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
                            © 2025 Flearn - Nền tảng học ngôn ngữ thông minh
                        </p>
                    </div>
                </div>
            </body>
            </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund request instruction email to {Email}", toEmail);
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
                var subject = "✅ Đã nhận yêu cầu hoàn tiền - Flearn";
                var body = $@"
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset='UTF-8'>
            </head>
            <body style='margin: 0; padding: 0; font-family: Arial, sans-serif; background-color: #f4f4f4;'>
                <div style='max-width: 600px; margin: 0 auto; background-color: #ffffff;'>
                    <div style='background: linear-gradient(135deg, #28a745 0%, #20c997 100%); padding: 40px 20px; text-align: center;'>
                        <h1 style='color: white; margin: 0; font-size: 28px;'>✅ Đã nhận yêu cầu</h1>
                    </div>
                    
                    <div style='padding: 40px 30px;'>
                        <h2 style='color: #2c3e50;'>Xin chào {userName}!</h2>
                        <p style='font-size: 16px; color: #555;'>
                            Chúng tôi đã nhận được yêu cầu hoàn tiền của bạn cho lớp học <strong>{className}</strong>.
                        </p>
                        <div style='background-color: #e7f3ff; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                            <p style='margin: 0; font-size: 14px; color: #555;'><strong>Mã yêu cầu:</strong> {refundRequestId}</p>
                        </div>
                        <p style='font-size: 16px; color: #555;'>
                            Yêu cầu của bạn đang được xem xét và sẽ được xử lý trong vòng 3-5 ngày làm việc.
                        </p>
                    </div>
                </div>
            </body>
            </html>";

                return await SendEmailAsync(toEmail, subject, body);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending refund confirmation email");
                return false;
            }
        }

    }
}
