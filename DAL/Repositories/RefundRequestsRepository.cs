using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class RefundRequestsRepository : GenericRepository<RefundRequest>, IRefundRequestsRepository
    {
        public RefundRequestsRepository(AppDbContext context) : base(context)
        {
        }
        public async Task<RefundRequest> GetByIdWithDetailsAsync(Guid refundRequestId)
        {
            return await _context.RefundRequests
                .Include(r => r.Student)
                .Include(r => r.TeacherClass)
                    .ThenInclude(tc => tc.Teacher)
                .Include(r => r.ClassEnrollment)
                .Include(r => r.ProcessedByAdmin)
                .FirstOrDefaultAsync(r => r.RefundRequestID == refundRequestId);
        }

        public async Task<RefundRequest> GetPendingRefundByEnrollmentAsync(Guid enrollmentId)
        {
            return await _context.RefundRequests
                .FirstOrDefaultAsync(r =>
                    r.EnrollmentID == enrollmentId &&
                    (r.Status == RefundRequestStatus.Pending ||
                     r.Status == RefundRequestStatus.UnderReview));
        }

        public async Task<List<RefundRequest>> GetRefundRequestsByStudentAsync(Guid studentId)
        {
            return await _context.RefundRequests
                .Include(r => r.TeacherClass)
                .Include(r => r.ClassEnrollment)
                .Where(r => r.StudentID == studentId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<RefundRequest>> GetRefundRequestsByStatusAsync(RefundRequestStatus status)
        {
            return await _context.RefundRequests
                .Include(r => r.Student)
                .Include(r => r.TeacherClass)
                .Include(r => r.ClassEnrollment)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<List<RefundRequest>> GetAllPendingRefundRequestsAsync()
        {
            return await _context.RefundRequests
                .Include(r => r.Student)
                .Include(r => r.TeacherClass)
                .Include(r => r.ClassEnrollment)
                .Where(r => r.Status == RefundRequestStatus.Pending ||
                           r.Status == RefundRequestStatus.UnderReview)
                .OrderBy(r => r.RequestedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<RefundRequest>> GetByLearnerIdAsync(Guid learnerId)
        {
            return await _context.RefundRequests
                .AsNoTracking()
                .Include(r => r.ClassEnrollment) // Include ClassEnrollment
                    .ThenInclude(ce => ce.Class) // Từ đó Include Class
                        .ThenInclude(c => c.Teacher) // Từ đó Include Teacher
                .Include(r => r.CourseEnrollment) // Include CourseEnrollment
                    .ThenInclude(ce => ce.Course) // Từ đó Include Course
                .Where(r => r.StudentID == learnerId)
                .OrderByDescending(r => r.RequestedAt)
                .ToListAsync();
        }
        public async Task<int> GetPendingCountAsync()
        {
            return await _context.RefundRequests
                .CountAsync(r => r.Status == RefundRequestStatus.Pending);
        }
    }
}
