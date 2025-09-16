using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ICourseSubmissionRepository : IGenericRepository<CourseSubmission>
    {
        Task<List<CourseSubmission>> GetSubmissionsByUserAsync(Guid userId);
        Task<List<CourseSubmission>> GetSubmissionsByCourseAsync(Guid courseId);
        Task<List<CourseSubmission>> GetPendingSubmissionsAsync();
        Task<List<CourseSubmission>> GetSubmissionsByStatusAsync(CourseSubmission.SubmissionStatus status);
        Task<CourseSubmission> GetSubmissionWithCourseAsync(Guid submissionId);
    }
}
