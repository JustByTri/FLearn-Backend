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
            await using var context = new AppDbContext();

            return await context.PurchaseDetails
                .AnyAsync(pd => pd.CourseId == courseId &&
                                pd.Purchase.UserId == userId &&
                                pd.Purchase.Status == PurchaseStatus.Completed);
        }
    }
}

