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
    public class TeacherClassRepository : GenericRepository<TeacherClass>, ITeacherClassRepository
    {
        public TeacherClassRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<TeacherClass>> GetTeacherClassesAsync(Guid teacherId)
        {
            return await _context.TeacherClasses
                .Include(tc => tc.Language)
                .Include(tc => tc.Teacher)
              
                    .ThenInclude(t => t!.TeacherProfile)
                .Include(tc => tc.Enrollments)
                .Include(tc => tc.Program)
                .Include(tc =>tc.Level)
                .Where(tc => tc.TeacherID == teacherId)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TeacherClass>> GetTeacherClassesByStatusAsync(Guid teacherId, ClassStatus status)
        {
            return await _context.TeacherClasses
                .Include(tc => tc.Language)
                .Include(tc => tc.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                .Include(tc => tc.Enrollments)
                .Include(tc => tc.Program)
                .Include(tc => tc.Level)
                .Where(tc => tc.TeacherID == teacherId && tc.Status == status)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<TeacherClass?> GetClassWithEnrollmentsAsync(Guid classId)
        {
            return await _context.TeacherClasses
                .Include(tc => tc.Language)
                .Include(tc => tc.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                .Include(tc => tc.Enrollments)

                    .ThenInclude(e => e.Student)
                    .Include(tc => tc.Program)
                .Include(tc => tc.Level)
                .FirstOrDefaultAsync(tc => tc.ClassID == classId);
        }

        public async Task<List<TeacherClass>> GetClassesForPayoutAsync()
        {
            return await _context.TeacherClasses
                .Include(tc => tc.Enrollments)
                .Where(tc => tc.Status == ClassStatus.Completed_PendingPayout)
            .ToListAsync();
        }

        public async Task<int> GetEnrollmentCountAsync(Guid classId)
        {
            return await _context.ClassEnrollments
                .CountAsync(e => e.ClassID == classId && e.Status == EnrollmentStatus.Paid);
        }
        public async Task<List<TeacherClass>> GetAvailableClassesAsync(Guid? languageId = null)
        {
            var query = _context.TeacherClasses
                .Include(tc => tc.Language)
                .Include(tc => tc.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                    .Include(tc => tc.Program)
                .Include(tc => tc.Level)
                .Include(tc => tc.Enrollments)
                .Where(tc => tc.Status == ClassStatus.Published &&
                             tc.StartDateTime > DateTime.UtcNow);

            if (languageId.HasValue)
            {
                query = query.Where(tc => tc.LanguageID == languageId.Value);
            }

            return await query
                .OrderBy(tc => tc.StartDateTime)
                .ToListAsync();
        }

        public async Task<int> GetAvailableClassesCountAsync(Guid? languageId = null)
        {
            var query = _context.TeacherClasses
                .Where(tc => tc.Status == ClassStatus.Published &&
                             tc.StartDateTime > DateTime.UtcNow);

            if (languageId.HasValue)
            {
                query = query.Where(tc => tc.LanguageID == languageId.Value);
            }

            return await query.CountAsync();
        }

        public async Task<List<TeacherClass>> GetAvailableClassesPaginatedAsync(Guid? languageId, int page, int pageSize)
        {
            var query = _context.TeacherClasses
                .Include(tc => tc.Language)
                .Include(tc => tc.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                    .Include(tc => tc.Program)
                .Include(tc => tc.Level)
                .Include(tc => tc.Enrollments)
                .Where(tc => tc.Status == ClassStatus.Published &&
                             tc.StartDateTime > DateTime.UtcNow);

            if (languageId.HasValue)
            {
                query = query.Where(tc => tc.LanguageID == languageId.Value);
            }

            return await query
                .OrderBy(tc => tc.StartDateTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }
        public async Task<List<TeacherClass>> GetClassesStartingBetween(DateTime startDate, DateTime endDate)
        {
            return await _context.TeacherClasses
                .Include(tc => tc.Language)
                .Include(tc => tc.Program)
                .Include(tc => tc.Level)
                .Include(tc => tc.Teacher)
                    .ThenInclude(t => t!.TeacherProfile)
                .Include(tc => tc.Enrollments)

                    .ThenInclude(e => e.Student)
                .Where(tc => tc.StartDateTime >= startDate &&
                             tc.StartDateTime <= endDate &&
                             (tc.Status == ClassStatus.Published || tc.Status == ClassStatus.Scheduled))
                .OrderBy(tc => tc.StartDateTime)
                .ToListAsync();
        }
    }
}
