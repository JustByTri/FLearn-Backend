using Common.DTO.ApiResponse;
using Common.DTO.Teacher.Response;

namespace BLL.IServices.Teacher
{
    public interface ITeacherService
    {
        Task<BaseResponse<TeacherProfileResponse>> GetTeacherProfileAsync(Guid userId);
    }
}
