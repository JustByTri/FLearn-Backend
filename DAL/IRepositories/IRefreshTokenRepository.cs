using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IRefreshTokenRepository : IGenericRepository<RefreshToken>
    {
        Task<RefreshToken> GetByTokenAsync(string token);
        Task<List<RefreshToken>> GetByUserIdAsync(Guid userId);
        Task<bool> RevokeTokenAsync(string token);
        Task<bool> RevokeAllUserTokensAsync(Guid userId);
        Task<List<RefreshToken>> GetExpiredTokensAsync();
    }
}
