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
    public class TeacherCredentialRepository : GenericRepository<TeacherCredential>, ITeacherCredentialRepository
    {
        public TeacherCredentialRepository(AppDbContext context) : base(context) { }

        public async Task<List<TeacherCredential>> GetCredentialsByUserAsync(Guid userId)
        {
            return await _context.TeacherCredentials
                .Include(tc => tc.User)
                .Include(tc => tc.Application)
                .Where(tc => tc.UserID == userId)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TeacherCredential>> GetCredentialsByApplicationAsync(Guid applicationId)
        {
            return await _context.TeacherCredentials
                .Include(tc => tc.User)
                .Include(tc => tc.Application)
                .Where(tc => tc.ApplicationID == applicationId)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<TeacherCredential>> GetCredentialsByTypeAsync(TeacherCredential.CredentialType type)
        {
            return await _context.TeacherCredentials
                .Include(tc => tc.User)
                .Include(tc => tc.Application)
                .Where(tc => tc.Type == type)
                .OrderByDescending(tc => tc.CreatedAt)
                .ToListAsync();
        }

        public async Task<TeacherCredential> GetCredentialByFileUrlAsync(string fileUrl)
        {
            return await _context.TeacherCredentials
                .Include(tc => tc.User)
                .Include(tc => tc.Application)
                .FirstOrDefaultAsync(tc => tc.CredentialFileUrl == fileUrl);
        }
    }
}
