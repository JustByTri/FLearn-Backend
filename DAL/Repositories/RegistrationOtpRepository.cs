using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class RegistrationOtpRepository : GenericRepository<RegistrationOtp>, IRegistrationOtpRepository
    {
        public RegistrationOtpRepository(AppDbContext context) : base(context) { }

        public async Task<RegistrationOtp> GetValidOtpAsync(string email, string otpCode)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();


            return await _context.Set<RegistrationOtp>()
                .Where(x => x.Email.ToLower() == normalizedEmail &&
                           x.OtpCode == otpCode &&
                           !x.IsUsed &&
                           x.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreateAt)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidateOtpsAsync(string email)
        {
            var normalizedEmail = email.Trim().ToLowerInvariant();

            var otps = await _context.Set<RegistrationOtp>()
                .Where(x => x.Email.ToLower() == normalizedEmail && !x.IsUsed && x.ExpireAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}
