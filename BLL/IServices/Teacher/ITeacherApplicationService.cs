using Common.DTO.Staff;
using Common.DTO.Teacher;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Teacher
{
    public interface ITeacherApplicationService
    {
        Task<TeacherApplicationDto> CreateApplicationAsync(Guid userId, CreateTeacherApplicationDto dto);
        Task<TeacherApplicationDto?> GetApplicationByUserAsync(Guid userId);
        Task<List<TeacherApplicationDto>> GetAllApplicationsAsync();
        Task<List<TeacherApplicationDto>> GetPendingApplicationsAsync();
        Task<TeacherApplicationDto?> GetApplicationByIdAsync(Guid applicationId);
        Task<bool> ReviewApplicationAsync(Guid reviewerId, ReviewApplicationDto dto);
        Task<bool> CanUserApplyAsync(Guid userId);
        Task<List<TeacherApplicationDto>> GetPendingApplicationsByLanguageAsync(Guid languageId);
        Task<List<TeacherApplicationDto>> GetApplicationsByLanguageAsync(Guid languageId);

    }
}
