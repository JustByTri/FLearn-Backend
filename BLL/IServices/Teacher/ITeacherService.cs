using Common.DTO.ApiResponse;
using Common.DTO.PayOut;
using Common.DTO.Teacher;
using Common.DTO.Teacher.Response;
using System.Threading.Tasks;

namespace BLL.IServices.Teacher
{
    public interface ITeacherService
    {
        Task<BaseResponse<TeacherProfileResponse>> GetTeacherProfileAsync(Guid userId);
        Task<BaseResponse<object>> CreatePayoutRequestAsync(Guid teacherId, CreatePayoutRequestDto requestDto);
        Task<BaseResponse<TeacherBankAccountDto>> AddBankAccountAsync(Guid teacherId, CreateBankAccountDto dto);
        Task<BaseResponse<IEnumerable<TeacherBankAccountDto>>> GetMyBankAccountsAsync(Guid teacherId);
    }
}

