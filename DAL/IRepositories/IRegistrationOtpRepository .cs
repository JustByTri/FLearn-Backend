using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IRegistrationOtpRepository : IGenericRepository<RegistrationOtp>
    {
        Task<RegistrationOtp> GetValidOtpAsync(string email, string otpCode);
        Task InvalidateOtpsAsync(string email);
    }
}
