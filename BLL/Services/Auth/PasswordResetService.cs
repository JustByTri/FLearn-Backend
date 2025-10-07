using BLL.IServices.Auth;
using DAL.Models;
using DAL.UnitOfWork;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace BLL.Services.Auth
{
    public class PasswordResetService : IPasswordResetService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetService> _logger;

        public PasswordResetService(IUnitOfWork unitOfWork, IEmailService emailService, ILogger<PasswordResetService> logger)
        {
            _unitOfWork = unitOfWork;
            _emailService = emailService;
            _logger = logger;
        }

        public async Task<bool> SendPasswordResetOtpAsync(string email, string ipAddress, string userAgent)
        {
            try
            {
                // 1. Kiểm tra user tồn tại (normalize email)
                var normalizedEmail = email.Trim().ToLowerInvariant();
                var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
                if (user == null)
                {
                    _logger.LogWarning("Password reset attempted for non-existent email: {Email} from IP: {IP}", email, ipAddress);
                    return false; // Không tiết lộ user có tồn tại hay không
                }

                // 2. Rate limiting - kiểm tra số lần gửi OTP gần đây
                var recentOtps = await GetRecentOtpCountAsync(normalizedEmail);
                if (recentOtps >= 3) // Max 3 OTP trong 1 giờ
                {
                    _logger.LogWarning("Too many OTP requests for email: {Email} from IP: {IP}", email, ipAddress);
                    throw new InvalidOperationException("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau 1 giờ.");
                }

                // 3. Hủy tất cả OTP cũ của email này
                await _unitOfWork.PasswordResetOtps.InvalidatePasswordResetOtpsAsync(normalizedEmail);

                // 4. Tạo OTP bảo mật hơn
                var otpCode = await GenerateSecureOtpAsync(normalizedEmail);

                // 5. Lưu OTP với thông tin bảo mật
                var passwordResetOtp = new PasswordResetOtp
                {
                    Id = Guid.NewGuid(),
                    Email = normalizedEmail, // ✅ Sử dụng normalized email
                    OtpCode = otpCode,
                    ExpireAt = DateTime.UtcNow.AddMinutes(5), // 5 phút
                    IpAddress = ipAddress,
                    UserAgent = userAgent,
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false
                };

                await _unitOfWork.PasswordResetOtps.CreateAsync(passwordResetOtp);

                // 6. Gửi email
                await _emailService.SendPasswordResetOtpAsync(normalizedEmail, user.UserName, otpCode);

                _logger.LogInformation("Password reset OTP sent for email: {Email} from IP: {IP}", email, ipAddress);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending password reset OTP for email: {Email}", email);
                throw;
            }
        }

        public async Task<bool> VerifyPasswordResetOtpAsync(string email, string otpCode, string ipAddress)
        {
            try
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();

                var otp = await _unitOfWork.PasswordResetOtps.GetValidPasswordResetOtpAsync(normalizedEmail, otpCode);

                if (otp == null)
                {
                    _logger.LogWarning("Invalid OTP attempt for email: {Email} with code: {Code} from IP: {IP}",
                                     email, otpCode, ipAddress);

                    // Track failed attempts
                    await TrackFailedAttemptAsync(normalizedEmail, ipAddress);
                    return false;
                }

                // ✅ Bảo mật bổ sung: Kiểm tra IP matching (optional)
                if (!string.IsNullOrEmpty(otp.IpAddress) && otp.IpAddress != ipAddress)
                {
                    _logger.LogWarning("OTP IP mismatch for email: {Email}. Original: {OriginalIP}, Current: {CurrentIP}",
                                     email, otp.IpAddress, ipAddress);
                    // Có thể cho phép hoặc từ chối tùy policy bảo mật
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying OTP for email: {Email}", email);
                return false;
            }
        }

        public async Task<bool> ResetPasswordWithOtpAsync(string email, string otpCode, string newPassword)
        {
            try
            {
                var normalizedEmail = email.Trim().ToLowerInvariant();

                // 1. Verify OTP
                var otp = await _unitOfWork.PasswordResetOtps.GetValidPasswordResetOtpAsync(normalizedEmail, otpCode);
                if (otp == null)
                {
                    _logger.LogWarning("Invalid OTP for password reset: {Email} with code: {Code}", email, otpCode);
                    throw new InvalidOperationException("Mã OTP không hợp lệ hoặc đã hết hạn.");
                }

                // 2. Get user
                var user = await _unitOfWork.Users.GetByEmailAsync(normalizedEmail);
                if (user == null || !user.Status)
                {
                    _logger.LogWarning("User not found or inactive for password reset: {Email}", email);
                    throw new InvalidOperationException("Tài khoản không tồn tại hoặc đã bị khóa.");
                }

                // 3. Mark OTP as used IMMEDIATELY để tránh replay attack
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
                await _unitOfWork.PasswordResetOtps.UpdateAsync(otp);

                // 4. Create new password hash
                var (newPasswordHash, newPasswordSalt) = CreatePasswordHash(newPassword);

                // 5. Update user password
                user.PasswordHash = newPasswordHash;
                user.PasswordSalt = newPasswordSalt;
                user.UpdatedAt = DateTime.UtcNow;

                await _unitOfWork.Users.UpdateAsync(user);

                // 6. Revoke all refresh tokens để force logout trên tất cả devices
                await _unitOfWork.RefreshTokens.RevokeAllUserTokensAsync(user.UserID);

                // 7. Invalidate all remaining OTPs for this email
                await _unitOfWork.PasswordResetOtps.InvalidatePasswordResetOtpsAsync(normalizedEmail);

                _logger.LogInformation("Password successfully reset for user: {Email}", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password for email: {Email}", email);
                throw;
            }
        }

        #region Private Helper Methods

        private async Task<string> GenerateSecureOtpAsync(string email)
        {
            string otpCode;
            bool isUnique;
            int maxAttempts = 10;
            int attempts = 0;

            do
            {
                // Tạo OTP 6 chữ số bảo mật
                using (var rng = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[4];
                    rng.GetBytes(bytes);
                    var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                    otpCode = (randomNumber % 900000 + 100000).ToString();
                }

                // Kiểm tra OTP unique cho email này
                isUnique = !await _unitOfWork.PasswordResetOtps.GetAllAsync()
                    .ContinueWith(task => task.Result.Any(otp =>
                        otp.Email.ToLowerInvariant() == email.ToLowerInvariant() &&
                        otp.OtpCode == otpCode &&
                        !otp.IsUsed &&
                        otp.ExpireAt > DateTime.UtcNow));

                attempts++;
            }
            while (!isUnique && attempts < maxAttempts);

            if (!isUnique)
            {
                // Fallback: sử dụng timestamp-based OTP
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                otpCode = timestamp.Substring(timestamp.Length - 6);
            }

            return otpCode;
        }

        private async Task<int> GetRecentOtpCountAsync(string email)
        {
            var oneHourAgo = DateTime.UtcNow.AddHours(-1);
            var allOtps = await _unitOfWork.PasswordResetOtps.GetAllAsync();

            return allOtps.Count(otp =>
                otp.Email.ToLowerInvariant() == email.ToLowerInvariant() &&
                otp.CreatedAt >= oneHourAgo);
        }

        private async Task TrackFailedAttemptAsync(string email, string ipAddress)
        {
            // Log failed attempt - có thể implement rate limiting ở đây
            _logger.LogWarning("Failed OTP attempt for email: {Email} from IP: {IP} at {Time}",
                             email, ipAddress, DateTime.UtcNow);

            // TODO: Implement failed attempt tracking in database if needed
            await Task.CompletedTask;
        }

        private (string hash, string salt) CreatePasswordHash(string password)
        {
            using (var hmac = new HMACSHA512())
            {
                var salt = Convert.ToBase64String(hmac.Key);
                var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
                return (hash, salt);
            }
        }

        #endregion
    }
}
