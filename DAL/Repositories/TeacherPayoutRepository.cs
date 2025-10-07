using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherPayoutRepository : GenericRepository<TeacherPayout>, ITeacherPayoutRepository
    {
        public TeacherPayoutRepository(AppDbContext context) : base(context) { }
    }
}
