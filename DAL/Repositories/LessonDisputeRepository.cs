using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class LessonDisputeRepository : GenericRepository<LessonDispute>, ILessonDisputeRepository
    {
        public LessonDisputeRepository(AppDbContext context) : base(context) { }
    }
}
