using DAL.Basic;
using DAL.Models;
using DAL.Type;

namespace DAL.IRepositories
{
    public interface IExerciseRepository : IGenericRepository<Exercise>
    {
        Task<List<Exercise>> GetExercisesByLessonAsync(Guid lessonId);
        Task<Exercise> GetExerciseByPositionAsync(Guid lessonId, int position);
        Task<List<Exercise>> GetExercisesByTypeAsync(ExerciseType type);
        Task<Exercise> GetNextExerciseAsync(Guid lessonId, int currentPosition);
        Task<Exercise> GetPreviousExerciseAsync(Guid lessonId, int currentPosition);
    }
}
