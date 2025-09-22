using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IUserSurveyRepository : IGenericRepository<UserSurvey>
    {
        Task<UserSurvey?> GetByUserIdAsync(Guid userId);
        Task<List<UserSurvey>> GetCompletedSurveysAsync();
        Task<bool> HasUserCompletedSurveyAsync(Guid userId);
    }
}
