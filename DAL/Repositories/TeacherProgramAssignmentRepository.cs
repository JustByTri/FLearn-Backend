using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherProgramAssignmentRepository : GenericRepository<TeacherProgramAssignment>, ITeacherProgramAssignmentRepository
    {
        public TeacherProgramAssignmentRepository(AppDbContext context) : base(context) { }
    }
}
