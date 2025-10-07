using DAL.Basic;
using DAL.Models;

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
