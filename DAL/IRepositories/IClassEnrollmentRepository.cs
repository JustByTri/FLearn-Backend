using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IClassEnrollmentRepository : IGenericRepository<ClassEnrollment>
    {
        Task<List<ClassEnrollment>> GetEnrollmentsByClassAsync(Guid classId);
        Task<List<ClassEnrollment>> GetEnrollmentsByStudentAsync(Guid studentId);
        Task<ClassEnrollment?> GetEnrollmentByStudentAndClassAsync(Guid studentId, Guid classId);
        Task<List<ClassEnrollment>> GetEnrollmentsByStatusAsync(EnrollmentStatus status);
        Task<int> GetEnrollmentCountByClassAsync(Guid classId);
        Task<List<ClassEnrollment>> GetEnrollmentsByStudentPaginatedAsync(Guid studentId, EnrollmentStatus? status = null, int page = 1, int pageSize = 10);
        Task<int> GetEnrollmentsCountByStudentAsync(Guid studentId, EnrollmentStatus? status = null);
        Task<ClassEnrollment> GetEnrollmentWithDetailsAsync(Guid enrollment);

    }
}
