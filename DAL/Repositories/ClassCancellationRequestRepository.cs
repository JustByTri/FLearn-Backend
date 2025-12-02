using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    /// <summary>
    /// Repository implementation cho ClassCancellationRequest
    /// </summary>
    public class ClassCancellationRequestRepository : GenericRepository<ClassCancellationRequest>, IClassCancellationRequestRepository
    {
        public ClassCancellationRequestRepository(AppDbContext context) : base(context) { }

        /// <summary>
        /// L?y các yêu c?u h?y l?p ?ang ch? duy?t theo ngôn ng?
        /// Manager ch? th?y các yêu c?u c?a ngôn ng? mình qu?n lý
        /// </summary>
        public async Task<IEnumerable<ClassCancellationRequest>> GetPendingRequestsByManagerLanguageAsync(Guid languageId)
        {
            return await _dbSet
                .Include(r => r.TeacherClass)
                .Include(r => r.Teacher)
                .Where(r => r.Status == CancellationRequestStatus.Pending &&
                           r.TeacherClass.LanguageID == languageId)
                .OrderBy(r => r.RequestedAt) // ?u tiên yêu c?u c? nh?t
                .ToListAsync();
        }

        /// <summary>
        /// L?y chi ti?t yêu c?u v?i ??y ?? thông tin liên quan
        /// </summary>
        public async Task<ClassCancellationRequest?> GetByIdWithDetailsAsync(Guid requestId)
        {
            return await _dbSet
                .Include(r => r.TeacherClass)
                .Include(r => r.Teacher)
                .Include(r => r.ProcessedByManager)
                .FirstOrDefaultAsync(r => r.CancellationRequestId == requestId);
        }

        /// <summary>
        /// Ki?m tra xem l?p ?ã có yêu c?u h?y pending ch?a
        /// Tránh t?o duplicate requests
        /// </summary>
        public async Task<bool> HasPendingRequestAsync(Guid classId)
        {
            return await _dbSet
                .AnyAsync(r => r.ClassId == classId && r.Status == CancellationRequestStatus.Pending);
        }
    }
}
