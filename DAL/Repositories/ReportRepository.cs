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
    public class ReportRepository : GenericRepository<Report>, IReportRepository
    {
        public ReportRepository(AppDbContext context) : base(context) { }

        public async Task<List<Report>> GetReportsByUserAsync(Guid userId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedCourse)
                .Include(r => r.ReportedLesson)
                .Where(r => r.UserID == userId)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();
        }

        public async Task<List<Report>> GetReportsByStatusAsync(Report.ReportStatus status)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedCourse)
                .Include(r => r.ReportedLesson)
                .Where(r => r.Status == status)
                .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();
        }

        public async Task<List<Report>> GetPendingReportsAsync()
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedCourse)
                .Include(r => r.ReportedLesson)
                .Where(r => r.Status == Report.ReportStatus.Pending)
                .OrderBy(r => r.ReportedAt)
                .ToListAsync();
        }

        public async Task<List<Report>> GetReportsByTypeAsync(string reportType)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedCourse)
                .Include(r => r.ReportedLesson)
                .Where(r => r.Reason.Contains(reportType))
                .OrderByDescending(r => r.ReportedAt)
            .ToListAsync();
        }

        public async Task<Report> GetReportWithDetailsAsync(Guid reportId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedUser)
                .Include(r => r.ReportedCourse)
                    .ThenInclude(c => c.Teacher)
                .Include(r => r.ReportedLesson)
                    .ThenInclude(l => l.CourseUnit)
                        .ThenInclude(cu => cu.Course)
                .FirstOrDefaultAsync(r => r.ReportID == reportId);
        }

        public async Task<List<Report>> GetReportsForCourseAsync(Guid courseId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedCourse)
                .Where(r => r.ReportedCourseID == courseId)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();
        }

        public async Task<List<Report>> GetReportsForLessonAsync(Guid lessonId)
        {
            return await _context.Reports
                .Include(r => r.Reporter)
                .Include(r => r.ReportedLesson)
                .Where(r => r.ReportedLessonID == lessonId)
                .OrderByDescending(r => r.ReportedAt)
                .ToListAsync();
        }
    }
}
