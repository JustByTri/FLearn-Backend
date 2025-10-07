using DAL.Basic;
using DAL.DBContext;
using DAL.IRepositories;
using DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace DAL.Repositories
{
    public class CourseTopicRepository : GenericRepository<CourseTopic>, ICourseTopicRepository
    {
        public CourseTopicRepository(AppDbContext context) : base(context) { }

        public async Task<List<CourseTopic>> GetTopicsByCourseAsync(Guid courseId)
        {
            return await _context.CourseTopics
                .Include(ct => ct.Course)
                .Include(ct => ct.Topic)
                .Where(ct => ct.CourseID == courseId)
                .ToListAsync();
        }

        public async Task<List<CourseTopic>> GetCoursesByTopicAsync(Guid topicId)
        {
            return await _context.CourseTopics
                .Include(ct => ct.Course)
                .Include(ct => ct.Topic)
                .Where(ct => ct.TopicID == topicId)
                .ToListAsync();
        }

        public async Task<bool> IsCourseTopicExistsAsync(Guid courseId, Guid topicId)
        {
            return await _context.CourseTopics
                .AnyAsync(ct => ct.CourseID == courseId && ct.TopicID == topicId);
        }

        public async Task<bool> RemoveCourseTopicAsync(Guid courseId, Guid topicId)
        {
            var courseTopic = await _context.CourseTopics
                .FirstOrDefaultAsync(ct => ct.CourseID == courseId && ct.TopicID == topicId);

            if (courseTopic != null)
            {
                _context.CourseTopics.Remove(courseTopic);
                await _context.SaveChangesAsync();
                return true;
            }
            return false;
        }
    }
}
