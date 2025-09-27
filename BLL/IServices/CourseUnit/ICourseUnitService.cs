using Common.DTO.ApiResponse;
using Common.DTO.CourseUnit.Request;
using Common.DTO.CourseUnit.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.CourseUnit
{
    public interface ICourseUnitService
    {
        Task<BaseResponse<UnitResponse>> CreateUnitAsync(Guid teacherId, Guid courseId, UnitRequest request);
        Task<BaseResponse<UnitResponse>> UpdateUnitAsync(Guid teacherId, Guid courseId, Guid unitId, UnitRequest request);
        Task<PagedResponse<IEnumerable<UnitResponse>>> GetUnitsAsync(PagingRequest request);
        Task<BaseResponse<UnitResponse>> GetUnitByIdAsync(Guid unitId);
        Task<PagedResponse<IEnumerable<UnitResponse>>> GetUnitsByCourseIdAsync(Guid courseId, PagingRequest request);
    }
}
