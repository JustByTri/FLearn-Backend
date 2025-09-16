using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ILanguageRepository : IGenericRepository<Language>
    {
        Task<Language> GetByLanguageCodeAsync(string languageCode);
        Task<List<Language>> GetActiveLanguagesAsync();
        Task<Language> GetByNameAsync(string languageName);
        Task<bool> IsLanguageCodeExistsAsync(string languageCode);
    }
}
