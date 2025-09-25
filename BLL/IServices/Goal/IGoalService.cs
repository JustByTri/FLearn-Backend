using Common.DTO.ApiResponse;
using Common.DTO.Goal.Request;
using Common.DTO.Goal.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;

namespace BLL.IServices.Goal
{
    public interface IGoalService
    {
        Task<BaseResponse<GoalResponse>> CreateAsync(GoalRequest request);
        Task<BaseResponse<GoalResponse>> UpdateAsync(int id, GoalRequest request);
        Task<BaseResponse<bool>> DeleteAsync(int id);
        Task<BaseResponse<GoalResponse>> GetByIdAsync(int id);
        Task<PagedResponse<IEnumerable<GoalResponse>>> GetAllAsync(PagingRequest request);
    }
}
