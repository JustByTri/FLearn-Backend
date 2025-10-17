using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ITeacherApplicationRepository : IGenericRepository<TeacherApplication>
    {
        Task<IEnumerable<TeacherApplication>> GetByUserIdAsync(Guid userId);
    }
}
