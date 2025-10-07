using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ICourseTopicRepository : IGenericRepository<CourseTopic>
    {
        Task<List<CourseTopic>> GetTopicsByCourseAsync(Guid courseId);
        Task<List<CourseTopic>> GetCoursesByTopicAsync(Guid topicId);
        Task<bool> IsCourseTopicExistsAsync(Guid courseId, Guid topicId);
        Task<bool> RemoveCourseTopicAsync(Guid courseId, Guid topicId);
    }
}
