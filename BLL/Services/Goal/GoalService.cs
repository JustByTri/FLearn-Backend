using BLL.IServices.Goal;
using Common.DTO.ApiResponse;
using Common.DTO.Goal.Request;
using Common.DTO.Goal.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Goal
{
    public class GoalService : IGoalService
    {
        private readonly IUnitOfWork _unit;
        public GoalService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<BaseResponse<GoalResponse>> CreateAsync(GoalRequest request)
        {
            var newGoal = new DAL.Models.Goal
            {
                Name = request.Name,
                Description = request.Description ?? "None",
            };

            _unit.Goals.Create(newGoal);
            await _unit.SaveChangesAsync();

            var response = new GoalResponse
            {
                Id = newGoal.Id,
                Name = request.Name,
                Description = request.Description ?? "None",
            };

            return BaseResponse<GoalResponse>.Success(response);
        }

        public Task<BaseResponse<bool>> DeleteAsync(int id)
        {
            throw new NotImplementedException();
        }

        public async Task<PagedResponse<IEnumerable<GoalResponse>>> GetAllAsync(PagingRequest request)
        {
            var query = _unit.Goals.Query();
            var totalItems = await query.CountAsync();

            var goals = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(g => new GoalResponse
                {
                    Id = g.Id,
                    Name = g.Name,
                    Description = g.Description ?? "None",
                })
                .OrderBy(g => g.Id)
                .ToListAsync();

            if (goals == null || !goals.Any())
            {
                return PagedResponse<IEnumerable<GoalResponse>>.Success(
                    new List<GoalResponse>(),
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "No goals found"
                );
            }

            return PagedResponse<IEnumerable<GoalResponse>>.Success(
                goals,
                request.Page,
                request.PageSize,
                totalItems,
                "Fetched goals successfully"
            );
        }

        public async Task<BaseResponse<GoalResponse>> GetByIdAsync(int id)
        {
            var selectedGoal = await _unit.Goals.GetByIdAsync(id);
            if (selectedGoal == null)
            {
                return BaseResponse<GoalResponse>.Fail(new { Id = "Not found" }, "Goal not found", 404);
            }

            var response = new GoalResponse
            {
                Id = selectedGoal.Id,
                Name = selectedGoal.Name,
                Description = selectedGoal.Description,
            };

            return BaseResponse<GoalResponse>.Success(response);
        }

        public async Task<BaseResponse<GoalResponse>> UpdateAsync(int id, GoalRequest request)
        {
            var selectedGoal = await _unit.Goals.GetByIdAsync(id);
            if (selectedGoal == null)
            {
                return BaseResponse<GoalResponse>.Fail(new { Id = "Not found" }, "Goal not found", 404);
            }

            selectedGoal.Name = request.Name;
            selectedGoal.Description = request.Description ?? "None";
            selectedGoal.UpdatedAt = DateTime.Now;

            await _unit.SaveChangesAsync();

            var response = new GoalResponse
            {
                Id = selectedGoal.Id,
                Name = selectedGoal.Name,
                Description = selectedGoal.Description,
            };

            return BaseResponse<GoalResponse>.Success(response);
        }
    }
}
