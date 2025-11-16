using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface IWalletRepository : IGenericRepository<Wallet>
    {
        Task<Wallet?> GetByTeacherIdAsync(Guid teacherId);
    }
}
