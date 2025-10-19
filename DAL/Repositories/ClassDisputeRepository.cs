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
    public class ClassDisputeRepository : GenericRepository<ClassDispute>, IClassDisputeRepository
    {
        public ClassDisputeRepository(AppDbContext context) : base(context)
        {
        }

        public async Task<List<ClassDispute>> GetDisputesByClassAsync(Guid classId)
        {
            return await _context.ClassDisputes
                .Include(d => d.Student)
                .Include(d => d.Class)
                .Include(d => d.Enrollment)
                .Where(d => d.ClassID == classId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ClassDispute>> GetDisputesByStudentAsync(Guid studentId)
        {
            return await _context.ClassDisputes
                .Include(d => d.Class)
                .Include(d => d.Enrollment)
                .Where(d => d.StudentID == studentId)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<ClassDispute>> GetDisputesByStatusAsync(DisputeStatus status)
        {
            return await _context.ClassDisputes
                .Include(d => d.Student)
                .Include(d => d.Class)
                .Include(d => d.Enrollment)
                .Where(d => d.Status == status)
                .OrderByDescending(d => d.CreatedAt)
                .ToListAsync();
        }

        public async Task<ClassDispute?> GetDisputeWithDetailsAsync(Guid disputeId)
        {
            return await _context.ClassDisputes
                .Include(d => d.Student)
                .Include(d => d.Class)
                .Include(d => d.Enrollment)
                .Include(d => d.ResolvedByAdmin)
                .FirstOrDefaultAsync(d => d.DisputeID == disputeId);
        }
    }
}
