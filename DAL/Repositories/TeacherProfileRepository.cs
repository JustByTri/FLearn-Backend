using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherProfileRepository : GenericRepository<TeacherProfile>, ITeacherProfileRepository
    {
        public TeacherProfileRepository(AppDbContext context) : base(context) { }
    }
}
