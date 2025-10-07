using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LessonReviewRepository : GenericRepository<LessonReview>, ILessonReviewRepository
    {
        public LessonReviewRepository(AppDbContext context) : base(context) { }
    }
}
