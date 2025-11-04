using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class LessonRepository : GenericRepository<Lesson>, ILessonRepository
    {
        public LessonRepository(AppDbContext context) : base(context) { }

        public async Task<List<Lesson>> GetLessonsByUnitAsync(Guid unitId)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .Where(l => l.CourseUnitID == unitId)
                .OrderBy(l => l.Position)
            .ToListAsync();
        }

        public async Task<Lesson> GetLessonWithExercisesAsync(Guid lessonId)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .Include(l => l.Exercises.OrderBy(e => e.Position))
                .FirstOrDefaultAsync(l => l.LessonID == lessonId);
        }

        public async Task<List<Lesson>> GetPublishedLessonsAsync(Guid unitId)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .Where(l => l.CourseUnitID == unitId)
                .OrderBy(l => l.Position)
                .ToListAsync();
        }

        public async Task<Lesson> GetLessonByPositionAsync(Guid unitId, int position)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .FirstOrDefaultAsync(l => l.CourseUnitID == unitId && l.Position == position);
        }

        public async Task<Lesson> GetNextLessonAsync(Guid unitId, int currentPosition)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .Where(l => l.CourseUnitID == unitId && l.Position > currentPosition)
                .OrderBy(l => l.Position)
                .FirstOrDefaultAsync();
        }

        public async Task<Lesson> GetPreviousLessonAsync(Guid unitId, int currentPosition)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .Where(l => l.CourseUnitID == unitId && l.Position < currentPosition)
                .OrderByDescending(l => l.Position)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Lesson>> GetByCourseIdAsync(Guid courseId)
        {
            return await _context.Lessons
                .Include(l => l.CourseUnit)
                .Where(l => l.CourseUnit.CourseID == courseId)
                .OrderByDescending(l => l.Position)
                .ToListAsync();
        }
    }
}
