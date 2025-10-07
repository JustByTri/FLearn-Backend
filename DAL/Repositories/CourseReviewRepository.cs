using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class CourseReviewRepository : GenericRepository<CourseReview>, ICourseReviewRepository
    {
        public CourseReviewRepository(AppDbContext context) : base(context) { }
    }
}
