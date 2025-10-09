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
        Task<BaseResponse<CourseTemplateResponse>> UpdateAsync(Guid id, CourseTemplateRequest request);
        Task<BaseResponse<CourseTemplateResponse>> GetByIdAsync(Guid id);
        Task<PagedResponse<IEnumerable<CourseTemplateResponse>>> GetAllAsync(PagingRequest request);
    }
}
