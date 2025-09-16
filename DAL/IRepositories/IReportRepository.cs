using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IReportRepository : IGenericRepository<Report>
    {
        Task<List<Report>> GetReportsByUserAsync(Guid userId);
        Task<List<Report>> GetReportsByStatusAsync(Report.ReportStatus status);
        Task<List<Report>> GetPendingReportsAsync();
        Task<List<Report>> GetReportsByTypeAsync(string reportType);
        Task<Report> GetReportWithDetailsAsync(Guid reportId);
        Task<List<Report>> GetReportsForCourseAsync(Guid courseId);
        Task<List<Report>> GetReportsForLessonAsync(Guid lessonId);
    }
}
