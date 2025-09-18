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
    public class TempRegistrationRepository : GenericRepository<TempRegistration>, ITempRegistrationRepository
    {
        public TempRegistrationRepository(AppDbContext context) : base(context) { }

        public async Task<TempRegistration> GetValidTempRegistrationAsync(string email, string otpCode)
        {
            return await _context.Set<TempRegistration>()
                .Where(x => x.Email == email && x.OtpCode == otpCode && !x.IsUsed && x.ExpireAt > DateTime.UtcNow)
                .FirstOrDefaultAsync();
        }

        public async Task InvalidateTempRegistrationsAsync(string email)
        {
            var tempRegs = await _context.Set<TempRegistration>()
                .Where(x => x.Email == email && !x.IsUsed && x.ExpireAt > DateTime.UtcNow)
                .ToListAsync();

            foreach (var tempReg in tempRegs)
            {
                tempReg.IsUsed = true;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<TempRegistration> GetByEmailAsync(string email)
        {
            return await _context.Set<TempRegistration>()
                .Where(x => x.Email == email && !x.IsUsed && x.ExpireAt > DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .FirstOrDefaultAsync();
        }
    }
}
