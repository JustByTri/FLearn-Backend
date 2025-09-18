using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRegistrationOtpRepository : IGenericRepository<RegistrationOtp>
    {
        Task<RegistrationOtp> GetValidOtpAsync(string email, string otpCode);
        Task InvalidateOtpsAsync(string email);
    }
}
