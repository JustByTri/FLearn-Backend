using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(AppDbContext context) : base(context) { }

    }
}
