using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Topic.Request;
using Common.DTO.Topic.Response;

namespace BLL.IServices.Topic
{
    public interface ITopicService
    {
        Task<PagedResponse<IEnumerable<TopicResponse>>> GetTopicsAsync(PagingRequest request);
        Task<BaseResponse<TopicResponse>> GetTopicByIdAsync(Guid topicId);
        Task<BaseResponse<TopicResponse>> CreateTopicAsync(TopicRequest request);
    }
}
