using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ITeacherApplicationRepository : IGenericRepository<TeacherApplication>
    {
        Task<TeacherApplication?> GetByUserIdAsync(Guid userId);
    }
}
