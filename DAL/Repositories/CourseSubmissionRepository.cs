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
    public class CourseSubmissionRepository : GenericRepository<CourseSubmission>, ICourseSubmissionRepository
    {
        public CourseSubmissionRepository(AppDbContext context) : base(context) { }

        public async Task<List<CourseSubmission>> GetSubmissionsByUserAsync(Guid userId)
        {
            return await _context.CourseSubmissions
                .Include(cs => cs.Course)
                .Include(cs => cs.Submitter)
                .Where(cs => cs.SubmittedBy == userId)
                .OrderByDescending(cs => cs.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<CourseSubmission>> GetSubmissionsByCourseAsync(Guid courseId)
        {
            return await _context.CourseSubmissions
                .Include(cs => cs.Course)
                .Include(cs => cs.Submitter)
                .Where(cs => cs.CourseID == courseId)
                .OrderByDescending(cs => cs.SubmittedAt)
            .ToListAsync();
        }

        public async Task<List<CourseSubmission>> GetPendingSubmissionsAsync()
        {
            return await _context.CourseSubmissions
                .Include(cs => cs.Course)
                .Include(cs => cs.Submitter)
                .Where(cs => cs.Status == CourseSubmission.SubmissionStatus.Pending)
                .OrderBy(cs => cs.SubmittedAt)
                .ToListAsync();
        }

        public async Task<List<CourseSubmission>> GetSubmissionsByStatusAsync(CourseSubmission.SubmissionStatus status)
        {
            return await _context.CourseSubmissions
                .Include(cs => cs.Course)
                .Include(cs => cs.Submitter)
                .Where(cs => cs.Status == status)
                .OrderByDescending(cs => cs.SubmittedAt)
                .ToListAsync();
        }

        public async Task<CourseSubmission> GetSubmissionWithCourseAsync(Guid submissionId)
        {
            return await _context.CourseSubmissions
                .Include(cs => cs.Course)
                    .ThenInclude(c => c.Teacher)
                .Include(cs => cs.Course)
                    .ThenInclude(c => c.Language)
                .Include(cs => cs.Submitter)
                .FirstOrDefaultAsync(cs => cs.CourseSubmissionID == submissionId);
        }
    }
}
