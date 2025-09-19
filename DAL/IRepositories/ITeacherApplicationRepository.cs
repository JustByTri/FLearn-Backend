using DAL.Basic;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DAL.IRepositories
{
    public interface ITeacherApplicationRepository : IGenericRepository<TeacherApplication>
    {
        Task<List<TeacherApplication>> GetApplicationsByUserAsync(Guid userId);
        Task<List<TeacherApplication>> GetPendingApplicationsAsync();
        Task<TeacherApplication> GetApplicationWithCredentialsAsync(Guid applicationId);
        Task<List<TeacherApplication>> GetApplicationsByStatusAsync(bool status);
        Task<TeacherApplication> GetLatestApplicationByUserAsync(Guid userId);
      
        Task<List<TeacherApplication>> GetApplicationsByLanguageAsync(Guid languageId);
    }
}
