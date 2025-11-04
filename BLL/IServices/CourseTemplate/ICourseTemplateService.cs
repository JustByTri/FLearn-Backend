using Common.DTO.ApiResponse;
using Common.DTO.CourseTemplate.Request;
using Common.DTO.CourseTemplate.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.CourseTemplate
{
    public interface ICourseTemplateService
    {
        Task<BaseResponse<CourseTemplateResponse>> CreateAsync(CourseTemplateRequest request);
        Task<BaseResponse<CourseTemplateResponse>> UpdateAsync(Guid templateId, CourseTemplateRequest request);
        Task<BaseResponse<CourseTemplateResponse>> GetByIdAsync(Guid templateId);
        Task<PagedResponse<IEnumerable<CourseTemplateResponse>>> GetAllAsync(PagingRequest request);
        Task<PagedResponse<IEnumerable<CourseTemplateResponse>>> GetTemplatesByProgramAndLevelPagedAsync(Guid programId, Guid levelId, PagingRequest request);
    }
}
