using BLL.IServices.Topic;
using Common.DTO.ApiResponse;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Common.DTO.Topic.Request;
using Common.DTO.Topic.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Topic
{
    [Route("api/topics")]
    [ApiController]
    public class TopicController : ControllerBase
    {
        private readonly ITopicService _topicService;
        public TopicController(ITopicService topicService)
        {
            _topicService = topicService;
        }
        /// <summary>
        /// Retrieves a paginated list of topics.
        /// </summary>
        /// <param name="request">Paging information: Page, PageSize</param>
        /// <returns>List of topics with paging info</returns>
        /// <response code="200">Successfully retrieved topics</response>
        /// <response code="404">No topics found</response>
        /// <response code="500">Server error while fetching topics</response>
        [HttpGet]
        [ProducesResponseType(typeof(PagedResponse<IEnumerable<TopicResponse>>), 200)]
        [ProducesResponseType(typeof(object), 404)]
        [ProducesResponseType(typeof(object), 500)]
        public async Task<IActionResult> GetTopics([FromQuery] PagingRequest request)
        {
            try
            {
                var response = await _topicService.GetTopicsAsync(request);

                if (response.Data == null || !response.Data.Any())
                {
                    return NotFound(new
                    {
                        Message = "No topics found",
                        Page = request.Page,
                        PageSize = request.PageSize,
                        TotalItems = response.Meta.TotalItems
                    });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while fetching topics",
                    Details = ex.Message
                });
            }
        }
        /// <summary>
        /// Creates a new topic.
        /// </summary>
        /// <param name="request">Topic data</param>
        /// <returns>Created topic or error message</returns>
        /// <response code="201">Topic created successfully</response>
        /// <response code="400">Validation error or topic already exists</response>
        /// <response code="500">Server error</response>
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<TopicResponse>), 201)]
        [ProducesResponseType(typeof(BaseResponse<TopicResponse>), 400)]
        [ProducesResponseType(typeof(BaseResponse<TopicResponse>), 500)]
        public async Task<IActionResult> CreateTopic([FromForm] TopicRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new BaseResponse<TopicResponse>
                {
                    Message = "Validation failed",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList()
                });
            }

            try
            {
                var result = await _topicService.CreateTopicAsync(request);

                if (result.Data == null)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetTopicById), new { id = result.Data.TopicId }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<TopicResponse>
                {
                    Message = "An error occurred while creating the topic.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        /// <summary>
        /// Get topic by id (for CreatedAtAction)
        /// </summary>
        [HttpGet("{topicId}")]
        [ProducesResponseType(typeof(TopicResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTopicById(Guid topicId)
        {
            var topic = await _topicService.GetTopicByIdAsync(topicId);
            if (topic == null)
                return NotFound();

            return Ok(topic);
        }
        /// <summary>
        /// Updates an existing topic by its ID.
        /// </summary>
        /// <param name="topicId">The ID of the topic to update.</param>
        /// <param name="request">The topic request containing new details.</param>
        /// <returns>A <see cref="BaseResponse{TopicResponse}"/> indicating success or failure of the update.</returns>
        /// <response code="200">Topic updated successfully.</response>
        /// <response code="400">Topic with the same name already exists or validation fails.</response>
        /// <response code="500">Internal server error.</response>
        [HttpPut("{topicId:guid}")]
        [ProducesResponseType(typeof(BaseResponse<TopicResponse>), 200)]
        [ProducesResponseType(typeof(BaseResponse<TopicResponse>), 400)]
        [ProducesResponseType(typeof(BaseResponse<TopicResponse>), 500)]
        public async Task<IActionResult> UpdateTopic(Guid topicId, [FromForm] TopicRequest request)
        {
            var result = await _topicService.UpdateTopicAsync(topicId, request);

            if (result.Data != null)
                return Ok(result);

            if (result.Code == 400)
                return BadRequest(result);

            return StatusCode(500, result);
        }
    }
}
