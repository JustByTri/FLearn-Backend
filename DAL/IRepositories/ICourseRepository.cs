using DAL.Basic;
using DAL.Models;
using DAL.Type;

namespace DAL.IRepositories
{
    public interface ICourseRepository : IGenericRepository<Course>
    {
        Task<List<Course>> GetCoursesByTeacherAsync(Guid teacherId);
        Task<List<Course>> GetCoursesByLanguageAsync(Guid languageId);
        Task<List<Course>> GetPublishedCoursesAsync();
        Task<Course> GetCourseWithUnitsAsync(Guid courseId);
        Task<List<Course>> SearchCoursesAsync(string searchTerm);
        Task<List<Course>> GetCoursesByStatusAsync(CourseStatus status);
        Task<List<Course>> GetCoursesByLevelAsync(string level);
        Task<List<Course>> GetCoursesByPriceRangeAsync(decimal minPrice, decimal maxPrice);
        Task<int> GetTotalCoursesCountAsync();
        Task<int> GetCoursesByStatusCountAsync(string status);
    }
}
