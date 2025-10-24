using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ITeacherClassRepository : IGenericRepository<TeacherClass>
    {
        Task<List<TeacherClass>> GetTeacherClassesAsync(Guid teacherId);
        Task<List<TeacherClass>> GetTeacherClassesByStatusAsync(Guid teacherId, ClassStatus status);
        Task<TeacherClass?> GetClassWithEnrollmentsAsync(Guid classId);
        Task<List<TeacherClass>> GetClassesForPayoutAsync();
        Task<int> GetEnrollmentCountAsync(Guid classId);
        Task<List<TeacherClass>> GetAvailableClassesAsync(Guid? languageId = null);
        Task<int> GetAvailableClassesCountAsync(Guid? languageId = null);
        Task<List<TeacherClass>> GetAvailableClassesPaginatedAsync(Guid? languageId, int page, int pageSize);
        Task<List<TeacherClass>> GetClassesStartingBetween(DateTime startDate, DateTime endDate);

    }
}
