using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ICourseUnitRepository : IGenericRepository<CourseUnit>
    {
        Task<List<CourseUnit>> GetUnitsByCourseAsync(Guid courseId);
        Task<CourseUnit> GetUnitWithLessonsAsync(Guid unitId);
        Task<List<CourseUnit>> GetUnitsByPositionAsync(Guid courseId, int position);
        Task<CourseUnit> GetNextUnitAsync(Guid courseId, int currentPosition);
        Task<CourseUnit> GetPreviousUnitAsync(Guid courseId, int currentPosition);
    }
}
