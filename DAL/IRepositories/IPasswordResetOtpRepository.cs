using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IPasswordResetOtpRepository : IGenericRepository<PasswordResetOtp>
    {
        Task<PasswordResetOtp> GetValidPasswordResetOtpAsync(string email, string otpCode);
        Task InvalidatePasswordResetOtpsAsync(string email);
        Task<string> GenerateUniqueOtpAsync(string email);
    }
}
