using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IUserGoalRepository : IGenericRepository<UserGoal>
    {
        Task<UserGoal?> GetByUserAndLanguageAsync(Guid userId, Guid languageId);
        Task<List<UserGoal>> GetByUserIdAsync(Guid userId);
        Task<bool> HasUserCompletedSurveyForLanguageAsync(Guid userId, Guid languageId);
        Task<bool> HasUserSkippedSurveyForLanguageAsync(Guid userId, Guid languageId);
        Task<bool> HasUserCompletedVoiceAssessmentAsync(Guid userId, Guid languageId);
        Task<UserGoal?> GetUserGoalWithDetailsAsync(Guid userGoalId);
        Task<List<UserGoal>> GetActiveUserGoalsAsync(Guid userId);
    }
}
