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
    public class EnrollmentRepository : GenericRepository<Enrollment>, IEnrollmentRepository
    {
        public EnrollmentRepository(AppDbContext context) : base(context) { }

        public async Task<List<Enrollment>> GetEnrollmentsByUserAsync(Guid userId)
        {
            return await _context.Enrollments
                .Where(e => e.UserID == userId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();
        }

        public async Task<List<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId)
        {
            return await _context.Enrollments
                .Where(e => e.CourseID == courseId)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();
        }

        public async Task<Enrollment> GetUserCourseEnrollmentAsync(Guid userId, Guid courseId)
        {
            return await _context.Enrollments
                .FirstOrDefaultAsync(e => e.UserID == userId && e.CourseID == courseId);
        }

        public async Task<List<Enrollment>> GetActiveEnrollmentsAsync(Guid userId)
        {
            return await _context.Enrollments
                .Where(e => e.UserID == userId && e.IsActive == true)
                .OrderByDescending(e => e.EnrolledAt)
                .ToListAsync();
        }

        public async Task<List<Enrollment>> GetCompletedEnrollmentsAsync(Guid userId)
        {
            return await _context.Enrollments
                .Where(e => e.UserID == userId && e.CompletedAt != null)
                .OrderByDescending(e => e.CompletedAt)
            .ToListAsync();
        }

        public async Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId)
        {
            return await _context.Enrollments
                .AnyAsync(e => e.UserID == userId && e.CourseID == courseId);
        }
    }
}
