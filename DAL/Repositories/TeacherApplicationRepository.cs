using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class TeacherApplicationRepository : GenericRepository<TeacherApplication>, ITeacherApplicationRepository
    {
        public TeacherApplicationRepository(AppDbContext context) : base(context) { }

        public async Task<TeacherApplication?> GetByUserIdAsync(Guid userId)
        {
            return await _context.TeacherApplications
                .Include(a => a.Language)
                .Include(a => a.User)
                .Include(a => a.Certificates)
                    .ThenInclude(c => c.CertificateType)
                .FirstOrDefaultAsync(a => a.UserID == userId);
        }
    }
}
