using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.Repositories
{
    public class ExerciseRepository : GenericRepository<Exercise>, IExerciseRepository
    {
        public ExerciseRepository(AppDbContext context) : base(context) { }

        public async Task<List<Exercise>> GetExercisesByLessonAsync(Guid lessonId)
        {
            return await _context.Exercises
                .Include(e => e.Lesson)
                .Where(e => e.LessonID == lessonId)
                .OrderBy(e => e.Position)
                .ToListAsync();
        }

        public async Task<Exercise> GetExerciseByPositionAsync(Guid lessonId, int position)
        {
            return await _context.Exercises
                .Include(e => e.Lesson)
                .FirstOrDefaultAsync(e => e.LessonID == lessonId && e.Position == position);
        }

        public async Task<List<Exercise>> GetExercisesByTypeAsync(Exercise.ExerciseType type)
        {
            return await _context.Exercises
                .Include(e => e.Lesson)
                .Where(e => e.Type == type)
                .OrderBy(e => e.Position)
                .ToListAsync();
        }

        public async Task<Exercise> GetNextExerciseAsync(Guid lessonId, int currentPosition)
        {
            return await _context.Exercises
                .Include(e => e.Lesson)
                .Where(e => e.LessonID == lessonId && e.Position > currentPosition)
                .OrderBy(e => e.Position)
                .FirstOrDefaultAsync();
        }

        public async Task<Exercise> GetPreviousExerciseAsync(Guid lessonId, int currentPosition)
        {
            return await _context.Exercises
                .Include(e => e.Lesson)
                .Where(e => e.LessonID == lessonId && e.Position < currentPosition)
                .OrderByDescending(e => e.Position)
                .FirstOrDefaultAsync();
        }
    }
}
