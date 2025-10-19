using Common.DTO.Learner;
using Common.DTO.Teacher;
using DAL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.IServices.Teacher
{
    public interface ITeacherClassService
    {
        Task<TeacherClassDto> CreateClassAsync(Guid teacherId, CreateClassDto createClassDto);
        Task<TeacherClassDto> UpdateClassAsync(Guid teacherId, Guid classId, UpdateClassDto updateClassDto);
        Task<bool> PublishClassAsync(Guid teacherId, Guid classId);
        Task<bool> CancelClassAsync(Guid teacherId, Guid classId, string reason);
        Task<List<TeacherClassDto>> GetTeacherClassesAsync(Guid teacherId, ClassStatus? status = null);
        Task<TeacherClassDto> GetClassDetailsAsync(Guid teacherId, Guid classId);
        Task<List<ClassEnrollmentDto>> GetClassEnrollmentsAsync(Guid teacherId, Guid classId);
    }
}
