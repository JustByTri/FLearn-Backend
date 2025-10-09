using BLL.IServices.CourseTemplate;
using Common.DTO.ApiResponse;
using Common.DTO.CourseTemplate.Request;
using Common.DTO.CourseTemplate.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Topic.Response;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.CourseTemplate
{
    [Route("api/coursetemplates")]
    [ApiController]
    public class CourseTemplateController : ControllerBase
    {
        private readonly ICourseTemplateService _courseTemplateService;
        public CourseTemplateController(ICourseTemplateService courseTemplateService)
        {
            _courseTemplateService = courseTemplateService;
        }
        /// <summary>
        /// Get all course templates with pagination.
        /// </summary>
        /// <param name="request">Paging parameters (Page, PageSize)</param>
        /// <returns>List of course templates</returns>
        [HttpGet]
        [ProducesResponseType(typeof(BaseResponse<IEnumerable<CourseTemplateResponse>>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] PagingRequest request)
        {
            var response = await _courseTemplateService.GetAllAsync(request);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Create a new course template.
        /// </summary>
        /// <param name="request">Course template data</param>
        /// <returns>Created course template</returns>
        [HttpPost]
        [ProducesResponseType(typeof(BaseResponse<CourseTemplateResponse>), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] CourseTemplateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new BaseResponse<CourseTemplateResponse>
                {
                    Message = "Validation failed",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList()
                });
            }

            try
            {
                var result = await _courseTemplateService.CreateAsync(request);

                if (result.Data == null)
                    return BadRequest(result);

                return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new BaseResponse<TopicResponse>
                {
                    Message = "An error occurred while creating the course template.",
                    Errors = new List<string> { ex.Message }
                });
            }
        }
        /// <summary>
        /// Get a course template by its Id.
        /// </summary>
        /// <param name="id">Course template Id</param>
        /// <returns>Course template details</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(BaseResponse<CourseTemplateResponse>), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(Guid id)
        {
            var response = await _courseTemplateService.GetByIdAsync(id);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Update an existing course template.
        /// </summary>
        /// <param name="id">Course template Id</param>
        /// <param name="request">Updated course template data</param>
        /// <returns>Updated course template</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(BaseResponse<CourseTemplateResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> Update(Guid id, [FromBody] CourseTemplateRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new BaseResponse<CourseTemplateResponse>
                {
                    Message = "Validation failed",
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                              .Select(e => e.ErrorMessage)
                                              .ToList()
                });
            }

            var response = await _courseTemplateService.UpdateAsync(id, request);
            return StatusCode(response.Code, response);
        }
    }
}
