using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class TeacherProfileRepository : GenericRepository<TeacherProfile>, ITeacherProfileRepository
    {
        private readonly AppDbContext _context;
        public TeacherProfileRepository(AppDbContext context) : base(context)
        {
            _context = context;
        }
        public async Task<TeacherProfile?> GetPublicProfileByIdAsync(Guid teacherProfileId)
        {
            return await _context.TeacherProfiles
                .AsNoTracking()
                .Include(tp => tp.User) 
                .Include(tp => tp.Courses) 
         
                    .ThenInclude(tc => tc.Enrollments) 
                .Include(tp => tp.TeacherReviews) 
                .Where(tp => tp.TeacherId == teacherProfileId &&
                             tp.User.Status == true && 
                             tp.Status == true)   
                .FirstOrDefaultAsync();
        }
        public async Task<TeacherProfile?> GetByUserIdAsync(Guid userId)
        {
            return await _context.TeacherProfiles.FirstOrDefaultAsync(t => t.UserId == userId);
        }
    }
}
