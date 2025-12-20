using BLL.IServices.Subscription;
using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Subscription.Request;
using Common.DTO.Subscription.Response;
using DAL.Helpers;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BLL.Services.Subscription
{
    public class SubscriptionPlanService : ISubscriptionPlanService
    {
        private readonly IUnitOfWork _unit;
        private readonly ILogger<SubscriptionPlanService> _logger;

        public SubscriptionPlanService(IUnitOfWork unit, ILogger<SubscriptionPlanService> logger)
        {
            _unit = unit;
            _logger = logger;
        }
        public async Task<PagedResponse<IEnumerable<SubscriptionPlanResponse>>> GetAllPlansAsync(PagingRequest request)
        {
            try
            {
                var query = await _unit.Subscriptions.GetAllAsync();

                var totalItems = query.Count();

                var plans = query
                    .OrderBy(s => s.ConversationQuota)
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(s => new SubscriptionPlanResponse
                    {
                        SubscriptionId = s.SubscriptionId,
                        Name = s.Name,
                        Price = s.Price,
                        ConversationQuota = s.ConversationQuota,
                        CreatedAt = s.CreatedAt.ToString("dd-MM-yyyy"),
                        UpdatedAt = s.UpdatedAt.ToString("dd-MM-yyyy")
                    });

                if (plans == null || !plans.Any())
                {
                    return PagedResponse<IEnumerable<SubscriptionPlanResponse>>.Success(
                        new List<SubscriptionPlanResponse>(),
                        request.Page,
                        request.PageSize,
                        totalItems,
                        "No subscription plans found"
                    );
                }

                return PagedResponse<IEnumerable<SubscriptionPlanResponse>>.Success(
                    plans,
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "Fetched plans successfully"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subscription plans");
                return PagedResponse<IEnumerable<SubscriptionPlanResponse>>.Success(
                        new List<SubscriptionPlanResponse>(),
                        request.Page,
                        request.PageSize,
                        0,
                        $"Error: {ex.Message}"
                    );
            }
        }
        public async Task<BaseResponse<SubscriptionPlanResponse>> GetPlanByIdAsync(int id)
        {
            try
            {
                var plan = await _unit.Subscriptions.GetByIdAsync(id);
                if (plan == null)
                {
                    return BaseResponse<SubscriptionPlanResponse>.Fail(null, "Gói đăng ký không tồn tại.", 404);
                }

                return BaseResponse<SubscriptionPlanResponse>.Success(MapToResponse(plan));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching plan {id}");
                return BaseResponse<SubscriptionPlanResponse>.Error(ex.Message);
            }
        }
        public async Task<BaseResponse<SubscriptionPlanResponse>> CreatePlanAsync(SubscriptionPlanRequest request)
        {
            try
            {
                var exists = await _unit.Subscriptions.Query()
                    .AnyAsync(s => s.Name.ToLower() == request.Name.Trim().ToLower());

                if (exists)
                {
                    return BaseResponse<SubscriptionPlanResponse>.Fail(null, $"Gói '{request.Name}' đã tồn tại.", 400);
                }

                var newPlan = new DAL.Models.Subscription
                {
                    Name = request.Name.Trim(),
                    Price = request.Price,
                    ConversationQuota = request.ConversationQuota,
                    CreatedAt = TimeHelper.GetVietnamTime(),
                    UpdatedAt = TimeHelper.GetVietnamTime()
                };

                await _unit.Subscriptions.CreateAsync(newPlan);
                await _unit.SaveChangesAsync();

                return BaseResponse<SubscriptionPlanResponse>.Success(MapToResponse(newPlan), "Gói đã được tạo thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating plan");
                return BaseResponse<SubscriptionPlanResponse>.Error(ex.Message);
            }
        }
        public async Task<BaseResponse<SubscriptionPlanResponse>> UpdatePlanAsync(int id, SubscriptionPlanRequest request)
        {
            try
            {
                var plan = await _unit.Subscriptions.GetByIdAsync(id);
                if (plan == null)
                {
                    return BaseResponse<SubscriptionPlanResponse>.Fail(null, "Gói đăng ký không tồn tại.", 404);
                }

                var isDuplicate = await _unit.Subscriptions.Query()
                    .AnyAsync(s => s.Name.ToLower() == request.Name.Trim().ToLower() && s.SubscriptionId != id);

                if (isDuplicate)
                {
                    return BaseResponse<SubscriptionPlanResponse>.Fail(null, $"Gói '{request.Name}' đã tồn tại.", 400);
                }

                plan.Name = request.Name.Trim();
                plan.Price = request.Price;
                plan.ConversationQuota = request.ConversationQuota;
                plan.UpdatedAt = TimeHelper.GetVietnamTime();

                await _unit.Subscriptions.UpdateAsync(plan);
                await _unit.SaveChangesAsync();

                return BaseResponse<SubscriptionPlanResponse>.Success(MapToResponse(plan), "Gói đã được cập nhật thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating plan {id}");
                return BaseResponse<SubscriptionPlanResponse>.Error(ex.Message);
            }
        }
        public async Task<BaseResponse<bool>> DeletePlanAsync(int id)
        {
            try
            {
                var plan = await _unit.Subscriptions.GetByIdAsync(id);
                if (plan == null)
                {
                    return BaseResponse<bool>.Fail(false, "Gói đăng ký không tồn tại.", 404);
                }

                _unit.Subscriptions.Remove(plan);
                await _unit.SaveChangesAsync();

                return BaseResponse<bool>.Success(true, "Gói đã được xóa thành công.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting plan {id}");
                return BaseResponse<bool>.Error(ex.Message);
            }
        }
        private SubscriptionPlanResponse MapToResponse(DAL.Models.Subscription s)
        {
            return new SubscriptionPlanResponse
            {
                SubscriptionId = s.SubscriptionId,
                Name = s.Name,
                Price = s.Price,
                ConversationQuota = s.ConversationQuota,
                CreatedAt = s.CreatedAt.ToString("dd-MM-yyyy"),
                UpdatedAt = s.UpdatedAt.ToString("dd-MM-yyyy")
            };
        }
    }
}
