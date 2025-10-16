using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class CourseGoalRepository : GenericRepository<CourseGoal>, ICourseGoalRepository
    {
        public CourseGoalRepository(AppDbContext context) : base(context) { }
    }
}
