using Common.DTO.Learner;
using Common.DTO.Teacher;
using DAL.Models;

namespace BLL.IServices.Teacher
{
    public interface ITeacherClassService
    {
        Task<TeacherClassDto> CreateClassAsync(Guid teacherId, CreateClassDto createClassDto);
        Task<TeacherClassDto> UpdateClassAsync(Guid teacherId, Guid classId, UpdateClassDto updateClassDto);
        Task<bool> PublishClassAsync(Guid teacherId, Guid classId);
        
        /// <summary>
        /// Hủy lớp học (Tự động nếu > 3 ngày, yêu cầu duyệt nếu ≤ 3 ngày)
        /// </summary>
        Task<bool> CancelClassAsync(Guid teacherId, Guid classId, string reason);
        
        /// <summary>
        /// Giáo viên gửi yêu cầu hủy lớp (cho trường hợp < 3 ngày trước khi bắt đầu)
        /// Returns: CancellationRequestId
        /// </summary>
        Task<Guid> RequestCancelClassAsync(Guid teacherId, Guid classId, string reason);
        
        Task<List<TeacherClassDto>> GetTeacherClassesAsync(Guid teacherId, ClassStatus? status = null);
        Task<TeacherClassDto> GetClassDetailsAsync(Guid teacherId, Guid classId);
        Task<List<ClassEnrollmentDto>> GetClassEnrollmentsAsync(Guid teacherId, Guid classId);
        Task<List<TeacherAssignmentDto>> GetMyProgramAssignmentsAsync(Guid teacherId);
        Task<bool> DeleteClassAsync(Guid teacherId, Guid classId);
    }
}
