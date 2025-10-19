using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface IClassDisputeRepository : IGenericRepository<ClassDispute>
    {
        Task<List<ClassDispute>> GetDisputesByClassAsync(Guid classId);
        Task<List<ClassDispute>> GetDisputesByStudentAsync(Guid studentId);
        Task<List<ClassDispute>> GetDisputesByStatusAsync(DisputeStatus status);
        Task<ClassDispute?> GetDisputeWithDetailsAsync(Guid disputeId);
    }
}
