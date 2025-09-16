using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ILessonRepository : IGenericRepository<Lesson>
    {
        Task<List<Lesson>> GetLessonsByUnitAsync(Guid unitId);
        Task<Lesson> GetLessonWithExercisesAsync(Guid lessonId);
        Task<List<Lesson>> GetPublishedLessonsAsync(Guid unitId);
        Task<Lesson> GetLessonByPositionAsync(Guid unitId, int position);
        Task<Lesson> GetNextLessonAsync(Guid unitId, int currentPosition);
        Task<Lesson> GetPreviousLessonAsync(Guid unitId, int currentPosition);
    }
}
