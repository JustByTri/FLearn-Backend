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

    public class RefreshTokenRepository : GenericRepository<RefreshToken>, IRefreshTokenRepository
    {
        public RefreshTokenRepository(AppDbContext context) : base(context) { }

        public async Task<RefreshToken> GetByTokenAsync(string token)
        {
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);
        }

        public async Task<List<RefreshToken>> GetByUserIdAsync(Guid userId)
        {
            return await _context.RefreshTokens
                .Where(rt => rt.UserID == userId && !rt.IsRevoked)
                .OrderByDescending(rt => rt.CreatedAt)
            .ToListAsync();
        }

        public async Task<bool> RevokeTokenAsync(string token)
        {
            var refreshToken = await _context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked);

            if (refreshToken != null)
            {
                refreshToken.IsRevoked = true;
                refreshToken.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> RevokeAllUserTokensAsync(Guid userId)
        {
            var tokens = await _context.RefreshTokens
                .Where(rt => rt.UserID == userId && !rt.IsRevoked)
                .ToListAsync();

            foreach (var token in tokens)
            {
                token.IsRevoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return tokens.Count > 0;
        }

        public async Task<List<RefreshToken>> GetExpiredTokensAsync()
        {
            return await _context.RefreshTokens
                .Where(rt => rt.ExpiresAt <= DateTime.UtcNow && !rt.IsRevoked)
                .ToListAsync();
        }
    }
}
