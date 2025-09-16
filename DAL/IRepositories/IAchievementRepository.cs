using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IAchievementRepository : IGenericRepository<Achievement>
    {
        Task<List<Achievement>> GetAchievementsByTitleAsync(string title);
        Task<Achievement> GetByTitleAsync(string title);
        Task<List<Achievement>> GetActiveAchievementsAsync();
    }
}
