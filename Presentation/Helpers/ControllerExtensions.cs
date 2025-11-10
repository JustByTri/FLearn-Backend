using Microsoft.AspNetCore.Mvc;
using Common.DTO.ApiResponse;
using System.Security.Claims;

namespace Presentation.Helpers
{
    public static class ControllerExtensions
    {
        public static bool TryGetUserId(this ControllerBase controller, out Guid userId, out IActionResult? errorResult)
        {
            userId = Guid.Empty;
            errorResult = null;

            var user = controller.User;

            var userIdClaim = user.FindFirstValue("user_id")
                           ?? user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim))
            {
                errorResult = controller.Unauthorized("User ID not found in token.");
                return false;
            }

            if (!Guid.TryParse(userIdClaim, out userId))
            {
                errorResult = controller.BadRequest("Invalid user ID format in token.");
                return false;
            }

            return true;
        }

        public static Guid GetUserId(this ControllerBase controller)
        {
            var userIdClaim = controller.User.FindFirstValue("user_id")
                               ?? controller.User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User ID not found in token.");
            }

            return userId;
        }

        public static IActionResult ToActionResult<T>(this ControllerBase controller, BaseResponse<T> response)
        {
            return response.Code switch
            {
                200 => controller.Ok(response),
                201 => controller.StatusCode(201, response),
                400 => controller.BadRequest(response),
                401 => controller.Unauthorized(response),
                403 => controller.StatusCode(403, response),
                404 => controller.NotFound(response),
                _ => controller.StatusCode(response.Code, response)
            };
        }
    }
}
