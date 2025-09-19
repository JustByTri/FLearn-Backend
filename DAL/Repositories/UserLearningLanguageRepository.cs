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
    public class UserLearningLanguageRepository : GenericRepository<UserLearningLanguage>, IUserLearningLanguageRepository
    {
        public UserLearningLanguageRepository(AppDbContext context) : base(context) { }

        public async Task<List<UserLearningLanguage>> GetLanguagesByUserAsync(Guid userId)
        {
            return await _context.UserLanguages
                .Where(ull => ull.UserID == userId)
                .ToListAsync();
        }

        public async Task<List<UserLearningLanguage>> GetUsersByLanguageAsync(Guid languageId)
        {
            return await _context.UserLanguages
                .Where(ull => ull.LanguageID == languageId)
                .ToListAsync();
        }

        public async Task<bool> IsUserLearningLanguageAsync(Guid userId, Guid languageId)
        {
            return await _context.UserLanguages
                .AnyAsync(ull => ull.UserID == userId && ull.LanguageID == languageId);
        }

        public async Task<UserLearningLanguage> GetUserLearningLanguageAsync(Guid userId, Guid languageId)
        {
            return await _context.UserLanguages
                .FirstOrDefaultAsync(ull => ull.UserID == userId && ull.LanguageID == languageId);
        }

        public async Task<bool> RemoveUserLearningLanguageAsync(Guid userId, Guid languageId)
        {
            var userLearningLanguage = await _context.UserLanguages
                .FirstOrDefaultAsync(ull => ull.UserID == userId && ull.LanguageID == languageId);

            if (userLearningLanguage != null)
            {
                _context.UserLanguages.Remove(userLearningLanguage);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
