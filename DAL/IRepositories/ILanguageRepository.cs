using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ILanguageRepository : IGenericRepository<Language>
    {
        Task<Language?> FindByLanguageCodeAsync(string langCode);
    }
}
