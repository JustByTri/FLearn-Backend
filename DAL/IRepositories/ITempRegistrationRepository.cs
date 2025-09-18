using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ITempRegistrationRepository : IGenericRepository<TempRegistration>
    {
        Task<TempRegistration> GetValidTempRegistrationAsync(string email, string otpCode);
        Task InvalidateTempRegistrationsAsync(string email);
        Task<TempRegistration> GetByEmailAsync(string email);
    }
}
