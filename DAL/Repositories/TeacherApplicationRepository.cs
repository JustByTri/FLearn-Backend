using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherApplicationRepository : GenericRepository<TeacherApplication>, ITeacherApplicationRepository
    {
        public TeacherApplicationRepository(AppDbContext context) : base(context) { }

    }
}
