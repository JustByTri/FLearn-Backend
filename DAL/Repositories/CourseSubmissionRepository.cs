using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class CourseSubmissionRepository : GenericRepository<CourseSubmission>, ICourseSubmissionRepository
    {
        public CourseSubmissionRepository(AppDbContext context) : base(context) { }

    }
}
