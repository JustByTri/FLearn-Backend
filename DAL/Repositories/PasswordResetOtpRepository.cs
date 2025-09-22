using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class PasswordResetOtpRepository : GenericRepository<PasswordResetOtp>, IPasswordResetOtpRepository
    {
        public PasswordResetOtpRepository(AppDbContext dbContext) : base(dbContext)
        {
        }

        public async Task<PasswordResetOtp> GetValidPasswordResetOtpAsync(string email, string otpCode)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            // ✅ KHẮC PHỤC: Chỉ lấy OTP của CHÍNH XÁC email đó
            return await _context.PasswordResetOtps
                .Where(otp => otp.Email.ToLower() == normalizedEmail &&  // ✅ Exact email match
                             otp.OtpCode == otpCode &&                     // ✅ Exact OTP match
                             !otp.IsUsed &&                                // ✅ Not used yet
                             otp.ExpireAt > DateTime.UtcNow)               // ✅ Not expired
                .OrderByDescending(otp => otp.CreatedAt)                   // ✅ Get latest
                .FirstOrDefaultAsync();
        }

        public async Task InvalidatePasswordResetOtpsAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var otps = await _context.PasswordResetOtps
                .Where(otp => otp.Email.ToLower() == normalizedEmail && !otp.IsUsed)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
                otp.UsedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<string> GenerateUniqueOtpAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();
            string otpCode;
            bool isUnique;
            int maxAttempts = 10;
            int attempts = 0;

            do
            {
                // Generate secure random OTP
                using (var rng = RandomNumberGenerator.Create())
                {
                    var bytes = new byte[4];
                    rng.GetBytes(bytes);
                    var randomNumber = Math.Abs(BitConverter.ToInt32(bytes, 0));
                    otpCode = (randomNumber % 900000 + 100000).ToString();
                }

                // Check uniqueness for THIS EMAIL only
                isUnique = !await _context.PasswordResetOtps
                    .AnyAsync(otp => otp.Email.ToLower() == normalizedEmail &&
                                   otp.OtpCode == otpCode &&
                                   !otp.IsUsed &&
                                   otp.ExpireAt > DateTime.UtcNow);

                attempts++;
            }
            while (!isUnique && attempts < maxAttempts);

            if (!isUnique)
            {
                // Fallback to timestamp-based unique code
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                otpCode = timestamp.Substring(Math.Max(0, timestamp.Length - 6)).PadLeft(6, '0');
            }

            return otpCode;
        }

        public async Task CleanupExpiredOtpsAsync()
        {
            var expiredOtps = await _context.PasswordResetOtps
                .Where(otp => otp.ExpireAt <= DateTime.UtcNow || otp.IsUsed)
                .ToListAsync();

            _context.PasswordResetOtps.RemoveRange(expiredOtps);
            await _context.SaveChangesAsync();
        }
    }
}

