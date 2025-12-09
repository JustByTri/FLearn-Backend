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
    public class ClassEnrollmentRepository : GenericRepository<ClassEnrollment>, IClassEnrollmentRepository
    {
        public ClassEnrollmentRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ClassEnrollment>> GetEnrollmentsByClassAsync(Guid classId)
        {
            return await _context.ClassEnrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Where(e => e.ClassID == classId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();
        }

        public async Task<List<ClassEnrollment>> GetEnrollmentsByStudentAsync(Guid studentId)
        {
            return await _context.ClassEnrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Teacher)
                        .ThenInclude(t => t!.TeacherProfile)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Language)
                .Where(e => e.StudentID == studentId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();
        }

        public async Task<ClassEnrollment?> GetEnrollmentByStudentAndClassAsync(Guid studentId, Guid classId)
        {
            return await _context.ClassEnrollments
                .Include(e => e.Class)
                .Include(e => e.Student)
                .FirstOrDefaultAsync(e => e.StudentID == studentId && e.ClassID == classId);
        }

        public async Task<List<ClassEnrollment>> GetEnrollmentsByStatusAsync(EnrollmentStatus status)
        {
            return await _context.ClassEnrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .Where(e => e.Status == status)
                .OrderByDescending(e => e.EnrolledAt)
            .ToListAsync();
        }

        public async Task<int> GetEnrollmentCountByClassAsync(Guid classId)
        {
            return await _context.ClassEnrollments
                .CountAsync(e => e.ClassID == classId && e.Status == EnrollmentStatus.Paid);
        }
        public async Task<List<ClassEnrollment>> GetEnrollmentsByStudentPaginatedAsync(Guid studentId, EnrollmentStatus? status = null, int page = 1, int pageSize = 10)
        {
            var query = _context.ClassEnrollments
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Language)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Teacher)
                        .ThenInclude(t => t!.TeacherProfile)
                .Include(e => e.Class)
                    .ThenInclude(c => c!.Enrollments)
                .Where(e => e.StudentID == studentId);

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            return await query
                .OrderByDescending(e => e.EnrolledAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetEnrollmentsCountByStudentAsync(Guid studentId, EnrollmentStatus? status = null)
        {
            var query = _context.ClassEnrollments
                .Where(e => e.StudentID == studentId);

            if (status.HasValue)
            {
                query = query.Where(e => e.Status == status.Value);
            }

            return await query.CountAsync();
        }
        public async Task<ClassEnrollment> GetEnrollmentWithDetailsAsync(Guid enrollmentId)
        {
            return await _context.ClassEnrollments
                .Include(e => e.Class)
                .Include(e => e.Student)
                .Include(e => e.RefundRequests)
                .FirstOrDefaultAsync(e => e.EnrollmentID == enrollmentId);
        }
    }
}
