using BLL.IServices.Topic;
using BLL.IServices.Upload;
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
        private readonly ICloudinaryService _cloudinary;
        public TopicService(IUnitOfWork unit, ICloudinaryService cloudinary)
        {
            _unit = unit;
            _cloudinary = cloudinary;
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
                    TopicDescription = t.Description,
                    ImageUrl = (t.ImageUrl != null) ? t.ImageUrl : "default"
                })
                .OrderBy(t => t.TopicName)
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
                    TopicDescription = t.Description,
                    ImageUrl = (t.ImageUrl != null) ? t.ImageUrl : "default"
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

            try
            {
                var result = await _cloudinary.UploadImageAsync(request.Image);

                var newTopic = new DAL.Models.Topic
                {
                    Name = request.Name.Trim(),
                    Description = request.Description,
                    ImageUrl = result.Url,
                    PublicId = result.PublicId,
                };

                _unit.Topics.Create(newTopic);
                await _unit.SaveChangesAsync();

                var response = new TopicResponse
                {
                    TopicId = newTopic.TopicID,
                    TopicName = newTopic.Name,
                    TopicDescription = newTopic.Description,
                    ImageUrl = result.Url
                };

                return BaseResponse<TopicResponse>.Success(response, "Topic created successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<TopicResponse>.Error(ex.Message);
            }
        }

        public async Task<BaseResponse<TopicResponse>> UpdateTopicAsync(Guid topicId, TopicRequest request)
        {
            var existingTopic = await _unit.Topics.Query()
                .FirstOrDefaultAsync(t => t.Name.ToLower() == request.Name.Trim().ToLower());

            if (existingTopic != null)
            {
                return BaseResponse<TopicResponse>.Fail
                (new object(), $"Topic with name '{request.Name}' already exists.", 400);
            }

            try
            {
                var selectedTopic = await _unit.Topics.GetByIdAsync(topicId);

                var uploadResult = await _cloudinary.UploadImageAsync(request.Image);

                var deleteResult = await _cloudinary.DeleteFileAsync(selectedTopic.PublicId);

                if (!deleteResult)
                {
                    return BaseResponse<TopicResponse>.Fail(new object(), "Failed to delete the image.");
                }

                selectedTopic.Name = request.Name.Trim();
                selectedTopic.Description = request.Description.Trim();
                selectedTopic.ImageUrl = uploadResult.Url;
                selectedTopic.PublicId = uploadResult.PublicId;

                await _unit.SaveChangesAsync();

                var response = new TopicResponse
                {
                    TopicId = selectedTopic.TopicID,
                    TopicName = selectedTopic.Name,
                    TopicDescription = selectedTopic.Description,
                    ImageUrl = uploadResult.Url
                };

                return BaseResponse<TopicResponse>.Success(response, "Topic updated successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<TopicResponse>.Error(ex.Message);
            }
        }
    }
}
