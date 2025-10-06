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
    public class UserGoalRepository : GenericRepository<UserGoal>, IUserGoalRepository
    {
        public UserGoalRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserGoal?> GetByUserAndLanguageAsync(Guid userId, Guid languageId)
        {
            return await _context.UserGoals
                .Include(ug => ug.User)
                .Include(ug => ug.Language)
                .Include(ug => ug.Goal)
                .FirstOrDefaultAsync(ug => ug.UserID == userId && ug.LanguageID == languageId && ug.IsActive);
        }

        public async Task<List<UserGoal>> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserGoals
                .Include(ug => ug.Language)
                .Include(ug => ug.Goal)
                .Where(ug => ug.UserID == userId && ug.IsActive)
                .OrderByDescending(ug => ug.UpdatedAt)
                .ToListAsync();
        }

        public async Task<bool> HasUserCompletedSurveyForLanguageAsync(Guid userId, Guid languageId)
        {
            return await _context.UserGoals
                .AnyAsync(ug => ug.UserID == userId &&
                              ug.LanguageID == languageId &&
                              ug.HasCompletedSurvey &&
                              ug.IsActive);
        }

        public async Task<bool> HasUserSkippedSurveyForLanguageAsync(Guid userId, Guid languageId)
        {
            return await _context.UserGoals
                .AnyAsync(ug => ug.UserID == userId &&
                              ug.LanguageID == languageId &&
                              ug.HasSkippedSurvey &&
                              ug.IsActive);
        }

        public async Task<bool> HasUserCompletedVoiceAssessmentAsync(Guid userId, Guid languageId)
        {
            return await _context.UserGoals
                .AnyAsync(ug => ug.UserID == userId &&
                              ug.LanguageID == languageId &&
                              ug.HasCompletedVoiceAssessment &&
                ug.IsActive);
        }

        public async Task<UserGoal?> GetUserGoalWithDetailsAsync(Guid userGoalId)
        {
            return await _context.UserGoals
                .Include(ug => ug.User)
                .Include(ug => ug.Language)
                .Include(ug => ug.Goal)
                .FirstOrDefaultAsync(ug => ug.UserGoalID == userGoalId);
        }

        public async Task<List<UserGoal>> GetActiveUserGoalsAsync(Guid userId)
        {
            return await _context.UserGoals
                .Include(ug => ug.Language)
                .Include(ug => ug.Goal)
                .Where(ug => ug.UserID == userId && ug.IsActive)
                .OrderByDescending(ug => ug.UpdatedAt)
                .ToListAsync();
        }
    }
}