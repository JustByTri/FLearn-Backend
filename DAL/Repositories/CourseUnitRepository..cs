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
    public class CourseUnitRepository : GenericRepository<CourseUnit>, ICourseUnitRepository
    {
        public CourseUnitRepository(AppDbContext context) : base(context) { }

        public async Task<List<CourseUnit>> GetUnitsByCourseAsync(Guid courseId)
        {
            return await _context.CourseUnits
                .Include(cu => cu.Course)
                .Include(cu => cu.Lessons)
                .Where(cu => cu.CourseID == courseId)
                .OrderBy(cu => cu.Position)
            .ToListAsync();
        }

        public async Task<CourseUnit> GetUnitWithLessonsAsync(Guid unitId)
        {
            return await _context.CourseUnits
                .Include(cu => cu.Course)
                .Include(cu => cu.Lessons.OrderBy(l => l.Position))
                .FirstOrDefaultAsync(cu => cu.CourseUnitID == unitId);
        }

        public async Task<List<CourseUnit>> GetUnitsByPositionAsync(Guid courseId, int position)
        {
            return await _context.CourseUnits
                .Include(cu => cu.Course)
                .Include(cu => cu.Lessons)
                .Where(cu => cu.CourseID == courseId && cu.Position == position)
                .ToListAsync();
        }

        public async Task<CourseUnit> GetNextUnitAsync(Guid courseId, int currentPosition)
        {
            return await _context.CourseUnits
                .Include(cu => cu.Course)
                .Include(cu => cu.Lessons)
                .Where(cu => cu.CourseID == courseId && cu.Position > currentPosition)
                .OrderBy(cu => cu.Position)
                .FirstOrDefaultAsync();
        }

        public async Task<CourseUnit> GetPreviousUnitAsync(Guid courseId, int currentPosition)
        {
            return await _context.CourseUnits
                .Include(cu => cu.Course)
                .Include(cu => cu.Lessons)
                .Where(cu => cu.CourseID == courseId && cu.Position < currentPosition)
                .OrderByDescending(cu => cu.Position)
                .FirstOrDefaultAsync();
        }
    }
}
