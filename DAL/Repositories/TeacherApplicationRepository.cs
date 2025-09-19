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
    public class TeacherApplicationRepository : GenericRepository<TeacherApplication>, ITeacherApplicationRepository
    {
        public TeacherApplicationRepository(AppDbContext context) : base(context) { }

        public async Task<List<TeacherApplication>> GetApplicationsByUserAsync(Guid userId)
        {
            return await _context.TeacherApplications
                .Include(ta => ta.User)
                .Where(ta => ta.UserID == userId)
                .OrderByDescending(ta => ta.AppliedAt)
                .ToListAsync();
        }

        public async Task<List<TeacherApplication>> GetPendingApplicationsAsync()
        {
            return await _context.TeacherApplications
                .Include(ta => ta.User)
                .Where(ta => ta.Status == false) // Assuming false means pending
                .OrderBy(ta => ta.AppliedAt)
                .ToListAsync();
        }

        public async Task<TeacherApplication> GetApplicationWithCredentialsAsync(Guid applicationId)
        {
            return await _context.TeacherApplications
                .Include(ta => ta.User)
                .Include(ta => ta.TeacherCredentials)
                .FirstOrDefaultAsync(ta => ta.TeacherApplicationID == applicationId);
        }

        public async Task<List<TeacherApplication>> GetApplicationsByStatusAsync(bool status)
        {
            return await _context.TeacherApplications
                .Include(ta => ta.User)
                .Where(ta => ta.Status == status)
                .OrderByDescending(ta => ta.AppliedAt)
                .ToListAsync();
        }

        public async Task<TeacherApplication?> GetLatestApplicationByUserAsync(Guid userId)
        {
            return await _context.TeacherApplications
                .Include(ta => ta.User)
                .Include(ta => ta.Language)
                .Where(ta => ta.UserID == userId)
                .OrderByDescending(ta => ta.AppliedAt)
                .FirstOrDefaultAsync();
        }

        public async Task<List<TeacherApplication>> GetApplicationsByLanguageAsync(Guid languageId)
        {
            return await _context.TeacherApplications
                .Include(ta => ta.User)
                .Include(ta => ta.Language)
                .Where(ta => ta.LanguageID == languageId)
                .OrderByDescending(ta => ta.AppliedAt)
                .ToListAsync();
        }
    }
}
