using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IExerciseRepository : IGenericRepository<Exercise>
    {
        Task<List<Exercise>> GetExercisesByLessonAsync(Guid lessonId);
        Task<Exercise> GetExerciseByPositionAsync(Guid lessonId, int position);
        Task<List<Exercise>> GetExercisesByTypeAsync(Exercise.ExerciseType type);
        Task<Exercise> GetNextExerciseAsync(Guid lessonId, int currentPosition);
        Task<Exercise> GetPreviousExerciseAsync(Guid lessonId, int currentPosition);
    }
}
