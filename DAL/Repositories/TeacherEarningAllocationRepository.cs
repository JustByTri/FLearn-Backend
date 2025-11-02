using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherEarningAllocationRepository : GenericRepository<TeacherEarningAllocation>, ITeacherEarningAllocationRepository
    {
        public TeacherEarningAllocationRepository(AppDbContext context) : base(context) { }
    }
}
