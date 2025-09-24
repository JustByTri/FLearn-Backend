using BLL.IServices.Topic;
using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Topic.Request;
using Common.DTO.Topic.Response;
using DAL.UnitOfWork;
using Microsoft.EntityFrameworkCore;

namespace BLL.Services.Topic
{
    public class TopicService : ITopicService
    {
        private readonly IUnitOfWork _unit;
        public TopicService(IUnitOfWork unit)
        {
            _unit = unit;
        }
        public async Task<PagedResponse<IEnumerable<TopicResponse>>> GetTopicsAsync(PagingRequest request)
        {
            var query = _unit.Topics.Query();
            var totalItems = await query.CountAsync();
            var topics = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(t => new TopicResponse
                {
                    TopicId = t.TopicID,
                    TopicName = t.Name,
                })
                .ToListAsync();

            if (topics == null || !topics.Any())
            {
                return PagedResponse<IEnumerable<TopicResponse>>.Success(
                    new List<TopicResponse>(),
                    request.Page,
                    request.PageSize,
                    totalItems,
                    "No topics found"
                );
            }

            return PagedResponse<IEnumerable<TopicResponse>>.Success(
                topics,
                request.Page,
                request.PageSize,
                totalItems,
                "Fetched topics successfully"
            );
        }
        public async Task<BaseResponse<TopicResponse>> GetTopicByIdAsync(Guid topicId)
        {
            try
            {
                var topic = await _unit.Topics.Query()
                .Where(t => t.TopicID == topicId)
                .Select(t => new TopicResponse
                {
                    TopicId = t.TopicID,
                    TopicName = t.Name,
                }).FirstOrDefaultAsync();

                if (topic == null)
                {
                    return BaseResponse<TopicResponse>.Fail(new object(), "Topic not found", 400);
                }
                else
                {
                    return BaseResponse<TopicResponse>.Success(topic);
                }
            }
            catch (Exception ex)
            {
                return BaseResponse<TopicResponse>.Error(ex.Message);
            }
        }
        public async Task<BaseResponse<TopicResponse>> CreateTopicAsync(TopicRequest request)
        {
            var existingTopic = await _unit.Topics.Query()
                .FirstOrDefaultAsync(t => t.Name.ToLower() == request.Name.Trim().ToLower());

            if (existingTopic != null)
            {
                return BaseResponse<TopicResponse>.Fail
                (new object(), $"Topic with name '{request.Name}' already exists.", 400);
            }

            var newTopic = new DAL.Models.Topic
            {
                Name = request.Name.Trim(),
                Description = request.Description,
            };

            _unit.Topics.Create(newTopic);
            await _unit.SaveChangesAsync();

            var response = new TopicResponse
            {
                TopicId = newTopic.TopicID,
                TopicName = newTopic.Name,
            };

            return BaseResponse<TopicResponse>.Success(response, "Topic created successfully.");
        }
    }
}
