using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        Task<bool> HasUserPurchasedCourseAsync(Guid userId, Guid courseId);
    }
}
