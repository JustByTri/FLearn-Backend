using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class LanguageRepository : GenericRepository<Language>, ILanguageRepository
    {
        public LanguageRepository(AppDbContext context) : base(context) { }

        public async Task<Language?> FindByLanguageCodeAsync(string langCode)
        {
            if (string.IsNullOrWhiteSpace(langCode))
                throw new ArgumentException("Language code must not be null or empty.", nameof(langCode));

            return await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.LanguageCode == langCode);
        }
    }
}
