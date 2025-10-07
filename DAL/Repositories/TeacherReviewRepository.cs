using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherReviewRepository : GenericRepository<TeacherReview>, ITeacherReviewRepository
    {
        public TeacherReviewRepository(AppDbContext context) : base(context) { }
    }
}
