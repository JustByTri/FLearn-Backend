using BLL.IServices.Goal;
using Common.DTO.Goal.Request;
using Common.DTO.Goal.Response;
using Common.DTO.Paging.Request;
using Microsoft.AspNetCore.Mvc;

namespace Presentation.Controllers.Goal
{
    [Route("api/goals")]
    [ApiController]
    public class GoalController : ControllerBase
    {
        private readonly IGoalService _goalService;
        public GoalController(IGoalService goalService)
        {
            _goalService = goalService;
        }
        /// <summary>
        /// Get all goals with paging
        /// </summary>
        /// <param name="request">Paging request</param>
        /// <returns>List of goals with pagination info</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<GoalResponse>), 200)]
        public async Task<IActionResult> GetAll([FromQuery] PagingRequest request)
        {
            var response = await _goalService.GetAllAsync(request);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Get goal by Id
        /// </summary>
        /// <param name="id">Goal Id</param>
        /// <returns>Goal detail</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(GoalResponse), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetById(int id)
        {
            var response = await _goalService.GetByIdAsync(id);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Create a new goal
        /// </summary>
        /// <param name="request">Goal data</param>
        /// <returns>Created goal</returns>
        [HttpPost]
        [ProducesResponseType(typeof(GoalResponse), 201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Create([FromBody] GoalRequest request)
        {
            var response = await _goalService.CreateAsync(request);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Update an existing goal
        /// </summary>
        /// <param name="id">Goal Id</param>
        /// <param name="request">Updated goal data</param>
        /// <returns>Updated goal</returns>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(GoalResponse), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> Update(int id, [FromBody] GoalRequest request)
        {
            var response = await _goalService.UpdateAsync(id, request);
            return StatusCode(response.Code, response);
        }

        /// <summary>
        /// Delete a goal by Id
        /// </summary>
        /// <param name="id">Goal Id</param>
        /// <returns>Delete result</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        public IActionResult Delete(int id)
        {
            return Content("The method or operation is not implemented.");
        }
    }
}
