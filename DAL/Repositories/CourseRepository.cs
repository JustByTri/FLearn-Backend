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
    }
}
