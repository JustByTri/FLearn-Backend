using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class RegistrationOtpRepository : GenericRepository<RegistrationOtp>, IRegistrationOtpRepository
    {
        public RegistrationOtpRepository(AppDbContext context) : base(context) { }

        public async Task<RegistrationOtp> GetValidOtpAsync(string email, string otpCode)
        {
            return await _context.Set<RegistrationOtp>()
                .Where(x => x.Email == email && x.OtpCode == otpCode && !x.IsUsed && x.ExpireAt > DateTime.UtcNow)
            .FirstOrDefaultAsync();
        }

        public async Task InvalidateOtpsAsync(string email)
        {
            var otps = await _context.Set<RegistrationOtp>()
                .Where(x => x.Email == email && !x.IsUsed && x.ExpireAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var otp in otps)
            {
                otp.IsUsed = true;
            }
            await _context.SaveChangesAsync();
        }
    }
}
