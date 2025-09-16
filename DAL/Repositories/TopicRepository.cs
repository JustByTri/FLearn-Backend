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
    public class TopicRepository : GenericRepository<Topic>, ITopicRepository
    {
        public TopicRepository(AppDbContext context) : base(context) { }

        public async Task<Topic> GetByNameAsync(string name)
        {
            return await _context.Topics
                .FirstOrDefaultAsync(t => t.Name == name);
        }

        public async Task<List<Topic>> SearchTopicsAsync(string searchTerm)
        {
            return await _context.Topics
                .Where(t => t.Name.Contains(searchTerm) || t.Description.Contains(searchTerm))
                .OrderBy(t => t.Name)
            .ToListAsync();
        }

        public async Task<bool> IsTopicNameExistsAsync(string name)
        {
            return await _context.Topics
                .AnyAsync(t => t.Name == name);
        }

        public async Task<List<Topic>> GetTopicsWithCoursesAsync()
        {
            return await _context.Topics
                .Include(t => t.CourseTopics)
                    .ThenInclude(ct => ct.Course)
                .OrderBy(t => t.Name)
                .ToListAsync();
        }
    }
}
