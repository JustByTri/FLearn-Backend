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
    public class UserSurveyRepository : GenericRepository<UserSurvey>, IUserSurveyRepository
    {
        public UserSurveyRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<UserSurvey?> GetByUserIdAsync(Guid userId)
        {
            return await _context.UserSurveys
                .Include(s => s.User)
                .Include(s => s.PreferredLanguage)
                .FirstOrDefaultAsync(s => s.UserID == userId);
        }

        public async Task<List<UserSurvey>> GetCompletedSurveysAsync()
        {
            return await _context.UserSurveys
                .Include(s => s.User)
                .Include(s => s.PreferredLanguage)
                .Where(s => s.IsCompleted)
                .OrderByDescending(s => s.CompletedAt)
            .ToListAsync();
        }

        public async Task<bool> HasUserCompletedSurveyAsync(Guid userId)
        {
            return await _context.UserSurveys
                .AnyAsync(s => s.UserID == userId && s.IsCompleted);
        }
    }
}

