using BLL.IServices.Application;
using Common.DTO.ApiResponse;
using Common.DTO.Application.Request;
using Common.DTO.Paging.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Helpers;
using System.Security.Claims;

namespace Presentation.Controllers.Application
{
    [Route("api")]
    [ApiController]
    public class ApplicationController : ControllerBase
    {
        private readonly ITeacherApplicationService _teacherApplicationService;
        public ApplicationController(ITeacherApplicationService teacherApplicationService)
        {
            _teacherApplicationService = teacherApplicationService;
        }
        /// <summary>
        /// Submits a new teacher application.
        /// </summary>
        /// <param name="request">The application form data containing personal information, certificates, and avatar.</param>
        /// <returns>
        /// A response containing the created application information if successful,
        /// or an error message if the operation fails.
        /// </returns>
        /// <remarks>
        /// This endpoint can only be accessed by users with the "Learner" role.
        /// The request should be sent as <c>multipart/form-data</c> to allow file uploads.
        /// </remarks>
        /// <response code="200">Returns the created application details.</response>
        /// <response code="400">If validation fails or the data is invalid.</response>
        /// <response code="401">If the user is not authorized.</response>
        [Authorize(Policy = "OnlyLearner")]
        [HttpPost("applications")]
        public async Task<IActionResult> Create([FromForm] ApplicationRequest request)
        {
            if (!this.TryGetUserId(out var userId, out var error))
                return error!;

            if (!ModelState.IsValid)
            {
                return BadRequest(BaseResponse<object>.Fail(ModelState));
            }

            var validationError = ValidateProficiency(request.LangCode, request.ProficiencyCode);
            if (validationError != null)
                return BadRequest(BaseResponse<object>.Fail(validationError));

            var result = await _teacherApplicationService.CreateApplicationAsync(userId, request);

            return StatusCode(result.Code, result);
        }
        private string? ValidateProficiency(string langCode, string proficiencyCode)
        {
            langCode = langCode.ToLower();

            return langCode switch
            {
                "en" => ValidateEnglishProficiency(proficiencyCode),
                "ja" => ValidateJapaneseProficiency(proficiencyCode),
                "zh" => ValidateChineseProficiency(proficiencyCode),
                _ => $"Unsupported language code: {langCode}. Allowed: en, ja, zh."
            };
        }
        private string? ValidateEnglishProficiency(string code)
        {
            var allowed = new[] { "A1", "A2", "B1", "B2", "C1", "C2" };

            if (!allowed.Contains(code.ToUpper()))
                return "Invalid proficiency code for English. Allowed: A1, A2, B1, B2, C1, C2.";

            return null;
        }
        private string? ValidateJapaneseProficiency(string code)
        {
            var allowed = new[] { "N5", "N4", "N3", "N2", "N1" };

            if (!allowed.Contains(code.ToUpper()))
                return "Invalid proficiency code for Japanese. Allowed: N5, N4, N3, N2, N1.";

            return null;
        }
        private string? ValidateChineseProficiency(string code)
        {
            var allowed = new[] { "HSK1", "HSK2", "HSK3", "HSK4", "HSK5", "HSK6" };

            if (!allowed.Contains(code.ToUpper()))
                return "Invalid proficiency code for Chinese. Allowed: HSK1–HSK6.";

            return null;
        }
        /// <summary>
        /// Get the paginated teacher applications of the currently logged-in user.
        /// </summary>
        /// <param name="request">Paging parameters (page, pageSize)</param>
        /// <param name="status">Optional filter by application status (Pending, Approved, Rejected)</param>
        /// <returns>Paginated list of user's teacher applications.</returns>
        [Authorize(Roles = "Learner")]
        [HttpGet("applications/me")]
        public async Task<IActionResult> GetMyApplications([FromQuery] PagingRequest request, [FromQuery] string? status)
        {
            var userIdClaim = User.FindFirstValue("user_id")
                                 ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized("User ID not found in token.");
            }

            if (!Guid.TryParse(userIdClaim, out Guid userId))
            {
                return BadRequest("Invalid user ID format in token.");
            }

            var result = await _teacherApplicationService.GetMyApplicationAsync(userId, request, status);

            return StatusCode(result.Code, result);
        }

        /// <summary>
        /// Updates an existing teacher application (only if it is still Pending or Rejected).
        /// </summary>
        /// <param name="request">The updated application form data.</param>
        /// <returns>Returns an updated application response if successful.</returns>
        /// <remarks>
        /// This endpoint allows resubmission of an application when the previous one was rejected or pending.
        /// Certificates and profile information will be revalidated and updated accordingly.
        /// </remarks>
        [Authorize(Policy = "OnlyLearner")]
        [HttpPut("applications")]
        public async Task<IActionResult> Update([FromForm] ApplicationUpdateRequest request)
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

            var result = await _teacherApplicationService.UpdateApplicationAsync(userId, request);

            return StatusCode(result.Code, result);
        }
        /// <summary>
        /// Get all teacher applications (Staff only)
        /// </summary>
        /// <param name="status">Optional: Filter by status (pending, approved, rejected)</param>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10)</param>
        /// <returns>Paginated list of teacher applications</returns>
        [Authorize(Roles = "Staff")]
        [HttpGet("staff/applications")]
        public async Task<IActionResult> GetAllApplications(
            [FromQuery] string? status,
            [FromQuery] PagingRequest request)
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

            var result = await _teacherApplicationService.GetApplicationAsync(userId, request, status);

            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Staff")]
        [HttpPut("staff/applications/{id}/approve")]
        public async Task<IActionResult> ApproveApplication(Guid id)
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
            var result = await _teacherApplicationService.ApproveApplicationAsync(userId, id);
            return StatusCode(result.Code, result);
        }
        [Authorize(Roles = "Staff")]
        [HttpPut("staff/applications/{id}/reject")]
        public async Task<IActionResult> RejectApplication(Guid id, [FromBody] RejectApplicationRequest request)
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
            var result = await _teacherApplicationService.RejectApplicationAsync(userId, id, request);
            return StatusCode(result.Code, result);
        }
    }
}
