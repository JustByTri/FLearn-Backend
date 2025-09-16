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
    public class LanguageRepository : GenericRepository<Language>, ILanguageRepository
    {
        public LanguageRepository(AppDbContext context) : base(context) { }

        public async Task<Language> GetByLanguageCodeAsync(string languageCode)
        {
            return await _context.Languages
                .FirstOrDefaultAsync(l => l.LanguageCode == languageCode);
        }

        public async Task<List<Language>> GetActiveLanguagesAsync()
        {
            return await _context.Languages
                .OrderBy(l => l.LanguageName)
            .ToListAsync();
        }

        public async Task<Language> GetByNameAsync(string languageName)
        {
            return await _context.Languages
                .FirstOrDefaultAsync(l => l.LanguageName == languageName);
        }

        public async Task<bool> IsLanguageCodeExistsAsync(string languageCode)
        {
            return await _context.Languages
                .AnyAsync(l => l.LanguageCode == languageCode);
        }
    }
}
