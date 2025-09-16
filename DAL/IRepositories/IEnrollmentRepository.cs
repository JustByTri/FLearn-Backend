using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IEnrollmentRepository : IGenericRepository<Enrollment>
    {
        Task<List<Enrollment>> GetEnrollmentsByUserAsync(Guid userId);
        Task<List<Enrollment>> GetEnrollmentsByCourseAsync(Guid courseId);
        Task<Enrollment> GetUserCourseEnrollmentAsync(Guid userId, Guid courseId);
        Task<List<Enrollment>> GetActiveEnrollmentsAsync(Guid userId);
        Task<List<Enrollment>> GetCompletedEnrollmentsAsync(Guid userId);
        Task<bool> IsUserEnrolledAsync(Guid userId, Guid courseId);
    }
}
