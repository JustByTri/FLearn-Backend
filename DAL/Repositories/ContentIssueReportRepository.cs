using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;

namespace DAL.Repositories
{
    public class ContentIssueReportRepository : GenericRepository<ContentIssueReport>, IContentIssueReportRepository
    {
        public ContentIssueReportRepository(AppDbContext context) : base(context) { }
    }
}
