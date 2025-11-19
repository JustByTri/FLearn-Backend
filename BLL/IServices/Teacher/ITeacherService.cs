using Common.DTO.ApiResponse;
using Common.DTO.Paging.Response;
using Common.DTO.PayOut;
using Common.DTO.Teacher;
using Common.DTO.Teacher.Request;
using Common.DTO.Teacher.Response;

namespace BLL.IServices.Teacher
{
    public interface ITeacherService
    {
        Task<BaseResponse<TeacherProfileResponse>> GetTeacherProfileAsync(Guid userId);
        Task<BaseResponse<object>> CreatePayoutRequestAsync(Guid teacherId, CreatePayoutRequestDto requestDto);
        Task<BaseResponse<TeacherBankAccountDto>> AddBankAccountAsync(Guid teacherId, CreateBankAccountDto dto);
        Task<BaseResponse<IEnumerable<TeacherBankAccountDto>>> GetMyBankAccountsAsync(Guid teacherId);
        Task<BaseResponse<PublicTeacherProfileDto>> GetPublicTeacherProfileAsync(Guid teacherId);
        Task<PagedResponse<IEnumerable<TeachingProgramResponse>>> GetTeachingProgramAsync(Guid userId, int pageNumber, int pageSize);
        Task<PagedResponse<List<TeacherSearchResponse>>> SearchTeachersAsync(TeacherSearchRequest request);
        Task<PagedResponse<IEnumerable<TeacherClassDto>>> SearchClassesAsync(Guid teacherId, string? keyword, string? status, DateTime? from, DateTime? to, Guid? programId, int page, int pageSize);
        Task<BaseResponse<TeacherProfileWithWalletResponse>> GetTeacherProfileWithWalletAsync(Guid userId);
        Task<BaseResponse<IEnumerable<TeacherProfileResponse>>> GetAllTeachersAsync();
        Task<PagedResponse<IEnumerable<TeacherClassDto>>> PublicSearchClassesAsync(Guid? languageId, Guid? teacherId, Guid? programId, string? keyword, string? status, DateTime? from, DateTime? to, int page, int pageSize);
        Task<PagedResponse<IEnumerable<PayoutRequestDto>>> GetMyPayoutRequestsAsync(Guid teacherId, string? status, DateTime? from, DateTime? to, int page, int pageSize);
        Task<BaseResponse<TeacherDashboardDto>> GetTeacherDashboardAsync(Guid teacherId, DateTime? from, DateTime? to, string? status, Guid? programId);
    }
}

