using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class CourseTemplateRepository : GenericRepository<CourseTemplate>, ICourseTemplateRepository
    {
        public CourseTemplateRepository(AppDbContext context) : base(context) { }
    }
}
