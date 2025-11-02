using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class TeacherBankAccountRepository : GenericRepository<TeacherBankAccount>, ITeacherBankAccountRepository
    {
        public TeacherBankAccountRepository(AppDbContext context) : base(context) { }
    }
}
