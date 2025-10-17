using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LessonActivityLogRepository : GenericRepository<LessonActivityLog>, ILessonActivityLogRepository
    {
        public LessonActivityLogRepository(AppDbContext context) : base(context) { }
    }
}
