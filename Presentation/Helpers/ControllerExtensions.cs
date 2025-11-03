using Microsoft.AspNetCore.Mvc;
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
    }
}
