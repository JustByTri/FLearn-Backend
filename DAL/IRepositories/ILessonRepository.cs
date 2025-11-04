using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ILessonRepository : IGenericRepository<Lesson>
    {
        Task<List<Lesson>> GetLessonsByUnitAsync(Guid unitId);
        Task<List<Lesson>> GetByCourseIdAsync(Guid courseId);
        Task<Lesson> GetLessonWithExercisesAsync(Guid lessonId);
        Task<List<Lesson>> GetPublishedLessonsAsync(Guid unitId);
        Task<Lesson> GetLessonByPositionAsync(Guid unitId, int position);
        Task<Lesson> GetNextLessonAsync(Guid unitId, int currentPosition);
        Task<Lesson> GetPreviousLessonAsync(Guid unitId, int currentPosition);
    }
}
