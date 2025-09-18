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
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context) { }

        public async Task<List<Course>> GetCoursesByTeacherAsync(Guid teacherId)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Include(c => c.CourseUnits)
                .Where(c => c.TeacherID == teacherId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Course>> GetCoursesByLanguageAsync(Guid languageId)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Where(c => c.LanguageID == languageId && c.Status == Course.CourseStatus.Published)
                .OrderByDescending(c => c.PublishedAt)
            .ToListAsync();
        }

        public async Task<List<Course>> GetPublishedCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Include(c => c.CourseUnits)
                .Where(c => c.Status == Course.CourseStatus.Published)
                .OrderByDescending(c => c.PublishedAt)
            .ToListAsync();
        }

        public async Task<Course> GetCourseWithUnitsAsync(Guid courseId)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Include(c => c.CourseUnits)
                    .ThenInclude(cu => cu.Lessons)
                .Include(c => c.CourseTopics)
                    .ThenInclude(ct => ct.Topic)
                .FirstOrDefaultAsync(c => c.CourseID == courseId);
        }

        public async Task<List<Course>> SearchCoursesAsync(string searchTerm)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Where(c => c.Status == Course.CourseStatus.Published &&
                           (c.Title.Contains(searchTerm) ||
                            c.Description.Contains(searchTerm) ||
                            c.Language.LanguageName.Contains(searchTerm)))
                .OrderByDescending(c => c.PublishedAt)
                .ToListAsync();
        }

        public async Task<List<Course>> GetCoursesByStatusAsync(Course.CourseStatus status)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Where(c => c.Status == status)
                .OrderByDescending(c => c.UpdatedAt)
                .ToListAsync();
        }

        public async Task<List<Course>> GetCoursesByLevelAsync(string level)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Where(c => c.Level == level && c.Status == Course.CourseStatus.Published)
                .OrderByDescending(c => c.PublishedAt)
                .ToListAsync();
        }

        public async Task<List<Course>> GetCoursesByPriceRangeAsync(decimal minPrice, decimal maxPrice)
        {
            return await _context.Courses
                .Include(c => c.Language)
                .Include(c => c.Teacher)
                .Where(c => c.Price >= minPrice && c.Price <= maxPrice && c.Status == Course.CourseStatus.Published)
                .OrderBy(c => c.Price)
                .ToListAsync();
        }
        public async Task<List<User>> GetAllUsersWithRolesAsync()
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetTotalUsersCountAsync()
        {
            return await _context.Users.CountAsync();
        }

        public async Task<int> GetActiveUsersCountAsync()
        {
            return await _context.Users.CountAsync(u => u.Status == true);
        }

        public async Task<int> GetUsersCountByRoleAsync(string roleName)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .CountAsync(u => u.UserRoles.Any(ur => ur.Role.Name == roleName));
        }

        public async Task<List<User>> GetRecentUsersAsync(int count)
        {
            return await _context.Users
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        public async Task<int> GetTotalCoursesCountAsync()
        {
            return await _context.Courses.CountAsync();
        }

        public async Task<int> GetCoursesByStatusCountAsync(string status)
        {
            if (Enum.TryParse<Course.CourseStatus>(status, out var courseStatus))
            {
                return await _context.Courses.CountAsync(c => c.Status == courseStatus);
            }
            return 0;
        }
    }
}

