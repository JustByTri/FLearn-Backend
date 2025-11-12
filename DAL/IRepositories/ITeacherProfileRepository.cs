using DAL.Basic;
using DAL.Models;

namespace DAL.IRepositories
{
    public interface ITeacherProfileRepository : IGenericRepository<TeacherProfile> {
        Task<TeacherProfile?> GetPublicProfileByIdAsync(Guid teacherProfileId);
    }



}
