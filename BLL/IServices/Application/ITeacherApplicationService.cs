using Common.DTO.ApiResponse;
using Common.DTO.Application.Request;
using Common.DTO.Application.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Application
{
    public interface ITeacherApplicationService
    {
        Task<BaseResponse<ApplicationResponse>> CreateApplicationAsync(Guid userId, ApplicationRequest applicationRequest);
        Task<BaseResponse<ApplicationResponse>> UpdateApplicationAsync(Guid userId, ApplicationUpdateRequest applicationRequest);
        Task<PagedResponse<IEnumerable<ApplicationResponse>>> GetMyApplicationAsync(Guid userId, PagingRequest request, string? status); // Application of that teacher
        Task<PagedResponse<IEnumerable<ApplicationResponse>>> GetApplicationAsync(Guid userId, PagingRequest request, string status); // Applications which staff manages
        Task<BaseResponse<ApplicationResponse>> GetApplicationByIdAsync(Guid applicationId);
        Task<BaseResponse<ApplicationResponse>> ApproveApplicationAsync(Guid userId, Guid applicationId);
        Task<BaseResponse<ApplicationResponse>> RejectApplicationAsync(Guid userId, Guid applicationId, RejectApplicationRequest request);
    }
}
