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
        public async Task<PagedResponse<IEnumerable<TopicResponse>>> GetTopicsAsync(PagingRequest request, bool isAdminView)
        {
            var query = _unit.Topics.Query();

            if (!isAdminView)
            {
                query = query.Where(t => t.Status == true);
            }
            else
            {
                query = query.OrderBy(t => t.Status)
                           .ThenBy(t => t.Name);
            }

            var totalItems = await query.CountAsync();

            if (!isAdminView)
            {
                query = query.OrderBy(t => t.Name);
            }

            var topics = await query
                    .Skip((request.Page - 1) * request.PageSize)
                    .Take(request.PageSize)
                    .Select(t => new TopicResponse
                    {
                        TopicId = t.TopicID,
                        TopicName = t.Name,
                        TopicDescription = t.Description,
                        ContextPrompt = t.ContextPrompt,
                        ImageUrl = (t.ImageUrl != null) ? t.ImageUrl : "default",
                        Status = t.Status
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
            if (existingTopic != null) return BaseResponse<TopicResponse>.Fail(new object(), $"Topic '{request.Name}' exists.", 400);

            try
            {
                if (request.Image == null) return BaseResponse<TopicResponse>.Fail(new object(), "Image is required for new topic.", 400);

                var result = await _cloudinary.UploadImageAsync(request.Image);

                var newTopic = new DAL.Models.Topic
                {
                    Name = request.Name.Trim(),
                    Description = request.Description,
                    ContextPrompt = request.ContextPrompt,
                    ImageUrl = result.Url,
                    PublicId = result.PublicId,
                    Status = false
                };

                _unit.Topics.Create(newTopic);
                await _unit.SaveChangesAsync();

                var response = MapToResponse(newTopic);
                return BaseResponse<TopicResponse>.Success(response, "Topic created successfully (Inactive status).");
            }
            catch (Exception ex)
            {
                return BaseResponse<TopicResponse>.Error(ex.Message);
            }
        }
        public async Task<BaseResponse<TopicResponse>> UpdateTopicAsync(Guid topicId, UpdateTopicRequest request)
        {
            try
            {
                var selectedTopic = await _unit.Topics.GetByIdAsync(topicId);
                if (selectedTopic == null)
                    return BaseResponse<TopicResponse>.Fail(new object(), "Topic not found", 404);

                if (!string.IsNullOrEmpty(request.Name))
                {
                    var isDuplicate = await _unit.Topics.Query()
                        .AnyAsync(t => t.Name.ToLower() == request.Name.Trim().ToLower()
                                       && t.TopicID != topicId);

                    if (isDuplicate)
                    {
                        return BaseResponse<TopicResponse>.Fail
                        (new object(), $"Topic with name '{request.Name}' already exists.", 400);
                    }

                    selectedTopic.Name = request.Name.Trim();
                }

                if (request.Description != null)
                {
                    selectedTopic.Description = request.Description.Trim();
                }

                if (!string.IsNullOrEmpty(request.ContextPrompt))
                {
                    selectedTopic.ContextPrompt = request.ContextPrompt;
                }

                if (request.Status.HasValue)
                {
                    selectedTopic.Status = request.Status.Value;
                }

                if (request.Image != null)
                {
                    var uploadResult = await _cloudinary.UploadImageAsync(request.Image);

                    if (!string.IsNullOrEmpty(selectedTopic.PublicId))
                    {
                        await _cloudinary.DeleteFileAsync(selectedTopic.PublicId);
                    }
                    selectedTopic.ImageUrl = uploadResult.Url;
                    selectedTopic.PublicId = uploadResult.PublicId;
                }

                await _unit.SaveChangesAsync();

                var response = MapToResponse(selectedTopic);
                return BaseResponse<TopicResponse>.Success(response, "Topic updated successfully.");
            }
            catch (Exception ex)
            {
                return BaseResponse<TopicResponse>.Error(ex.Message);
            }
        }
        public async Task<BaseResponse<bool>> DeleteTopicAsync(Guid topicId)
        {
            try
            {
                var topic = await _unit.Topics.GetByIdAsync(topicId);
                if (topic == null)
                {
                    return BaseResponse<bool>.Fail(false, "Topic not found", 404);
                }

                var isUsed = await _unit.CourseTopics.Query().AnyAsync(ct => ct.TopicID == topicId);
                if (isUsed) return BaseResponse<bool>.Fail(false, "Cannot delete topic because it is assigned to a course.", 400);

                if (!string.IsNullOrEmpty(topic.PublicId))
                {
                    var deleteImageResult = await _cloudinary.DeleteFileAsync(topic.PublicId);
                    if (!deleteImageResult)
                    {
                        Console.WriteLine($"Failed to delete image for topic {topicId}");
                    }
                }

                _unit.Topics.Remove(topic);
                await _unit.SaveChangesAsync();

                return BaseResponse<bool>.Success(true, "Topic deleted successfully.");
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null && ex.InnerException.Message.Contains("REFERENCE constraint"))
                {
                    return BaseResponse<bool>.Fail(false, "Cannot delete this topic because it is being used in other courses or sessions.", 400);
                }
                return BaseResponse<bool>.Error(ex.Message);
            }
        }
        #region
        private TopicResponse MapToResponse(DAL.Models.Topic t)
        {
            return new TopicResponse
            {
                TopicId = t.TopicID,
                TopicName = t.Name,
                TopicDescription = t.Description ?? "",
                ContextPrompt = t.ContextPrompt ?? "",
                ImageUrl = t.ImageUrl ?? "default",
                Status = t.Status
            };
        }
    }
    #endregion
}
