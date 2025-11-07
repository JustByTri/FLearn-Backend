using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using DAL.Type;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class CourseRepository : GenericRepository<Course>, ICourseRepository
    {
        public CourseRepository(AppDbContext context) : base(context) { }
        public async Task<bool> HasUserPurchasedCourseAsync(Guid userId, Guid courseId)
        {
            return await _context.Purchases
                .AnyAsync(p => p.CourseId == courseId &&
                               p.UserId == userId &&
                               p.Status == PurchaseStatus.Completed);
        }
        public async Task<IEnumerable<Course>> GetPopularCoursesAsync(int count)
        {
            return await _context.Courses
                .AsNoTracking()
                .Include(c => c.Teacher) 
                    .ThenInclude(t => t.User) 
                  .Include(c => c.Language)
                  .Include(c => c.Program)
                .Include(c => c.Level)
                .Where(c => c.Status == CourseStatus.Published) 
                .OrderByDescending(c => c.LearnerCount)
                .Take(count) 
                .ToListAsync();
        }
    }
}

