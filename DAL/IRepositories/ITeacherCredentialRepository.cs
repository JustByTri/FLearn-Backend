using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ITeacherCredentialRepository : IGenericRepository<TeacherCredential>
    {
        Task<List<TeacherCredential>> GetCredentialsByUserAsync(Guid userId);
        Task<List<TeacherCredential>> GetCredentialsByApplicationAsync(Guid applicationId);
        Task<List<TeacherCredential>> GetCredentialsByTypeAsync(TeacherCredential.CredentialType type);
        Task<TeacherCredential> GetCredentialByFileUrlAsync(string fileUrl);
    }
}
