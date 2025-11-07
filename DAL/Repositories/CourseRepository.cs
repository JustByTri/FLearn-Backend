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
    }
}

