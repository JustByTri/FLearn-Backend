using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IUserLearningLanguageRepository : IGenericRepository<UserLearningLanguage>
    {
        Task<List<UserLearningLanguage>> GetLanguagesByUserAsync(Guid userId);
        Task<List<UserLearningLanguage>> GetUsersByLanguageAsync(Guid languageId);
        Task<bool> IsUserLearningLanguageAsync(Guid userId, Guid languageId);
        Task<UserLearningLanguage> GetUserLearningLanguageAsync(Guid userId, Guid languageId);
        Task<bool> RemoveUserLearningLanguageAsync(Guid userId, Guid languageId);
    }
}
