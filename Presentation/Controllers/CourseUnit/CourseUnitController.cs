using BLL.IServices.CourseUnit;
using Common.DTO.ApiResponse;
using Common.DTO.CourseUnit.Request;
using Common.DTO.CourseUnit.Response;
using Common.DTO.Paging.Request;
using Common.DTO.Paging.Response;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Presentation.Controllers.CourseUnit
{
    [Route("api")]
    [ApiController]
    public class CourseUnitController : ControllerBase
    {
        private readonly ICourseUnitService _courseUnitService;
        public CourseUnitController(ICourseUnitService courseUnitService)
        {
            _courseUnitService = courseUnitService;
        }
        /// <summary>
        /// Creates a new Unit for a Course.
        /// </summary>
        /// <param name="courseId">ID of the course to which the Unit will be added.</param>
        /// <param name="request">Information about the Unit to be created.</param>
        /// <returns>Returns the information of the newly created Unit or an error message.</returns>
        /// <remarks>
        /// Example request:
        /// 
        ///     POST /api/courses/{courseId}/units
        ///     {
        ///         "title": "Unit 1 - Introduction",
        ///         "description": "Overview of the course"
        ///     }
        /// 
        /// </remarks>
        /// <response code="200">Successfully created a new Unit.</response>
        /// <response code="400">Invalid data or creation failed.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPost("courses/{courseId:guid}/units")]
        public async Task<IActionResult> CreateUnitAsync(Guid courseId, [FromBody] UnitRequest request)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var response = await _courseUnitService.CreateUnitAsync(userId, courseId, request);

            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Updates an existing unit within a specific course.
        /// </summary>
        /// <param name="courseId">The ID of the course containing the unit.</param>
        /// <param name="unitId">The ID of the unit to update.</param>
        /// <param name="request">The request payload containing updated unit details.</param>
        /// <returns>A <see cref="BaseResponse{UnitResponse}"/> containing the updated unit.</returns>
        /// <response code="200">Returns the updated unit.</response>
        /// <response code="400">If the request is invalid or update fails.</response>
        [Authorize(Roles = "Teacher")]
        [HttpPut("courses/{courseId:guid}/units")]
        public async Task<ActionResult<BaseResponse<UnitResponse>>> UpdateUnitAsync(
            Guid courseId,
            Guid unitId,
            [FromBody] UnitUpdateRequest request)
        {

            var userIdClaim = User.FindFirstValue("user_id")
                     ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("Teacher ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail("Invalid request data."));
            }

            var response = await _courseUnitService.UpdateUnitAsync(userId, courseId, unitId, request);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Retrieves a paginated list of units for a specific course.
        /// </summary>
        /// <param name="courseId">The ID of the course to filter units.</param>
        /// <param name="pageNumber">The current page number (default is 1).</param>
        /// <param name="pageSize">The number of records per page (default is 10).</param>
        /// <returns>
        /// A <see cref="PagedResponse{T}"/> containing the paginated list of <see cref="UnitResponse"/>.
        /// </returns>
        /// <response code="200">Returns a paginated list of course units for the specified course.</response>
        /// <response code="404">If the course is not found.</response>
        [HttpGet("courses/{courseId:guid}/units")]
        public async Task<ActionResult<PagedResponse<IEnumerable<UnitResponse>>>> GetUnitsByCourseIdAsync(Guid courseId, [FromQuery] PagingRequest request)
        {
            var response = await _courseUnitService.GetUnitsByCourseIdAsync(courseId, request);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Retrieves a unit by its unique identifier.
        /// </summary>
        /// <param name="unitId">The ID of the unit to retrieve.</param>
        /// <returns>A <see cref="BaseResponse{UnitResponse}"/> containing the unit details.</returns>
        /// <response code="200">Returns the requested unit.</response>
        /// <response code="404">If the unit is not found.</response>
        [HttpGet("units/{unitId:guid}")]
        public async Task<ActionResult<BaseResponse<UnitResponse>>> GetUnitByIdAsync(Guid unitId)
        {
            var response = await _courseUnitService.GetUnitByIdAsync(unitId);
            return StatusCode(response.Code, response);
        }
        /// <summary>
        /// Delete a unit
        /// </summary>
        [HttpDelete("units/{unitId:guid}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DeleteUnit(Guid unitId)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                             ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out Guid userId))
            {
                return Unauthorized("Invalid user ID");
            }

            var result = await _courseUnitService.DeleteUnitAsync(userId, unitId);
            return StatusCode(result.Code, result);
        }
    }
}
