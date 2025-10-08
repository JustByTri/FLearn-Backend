using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace Common.Authorization
{
    public class ExclusiveRoleHandler : AuthorizationHandler<ExclusiveRoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, ExclusiveRoleRequirement requirement)
        {
            var roles = context.User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToList();

            if (roles.Count == 1 && roles.Contains(requirement.RequiredRole))
            {
                context.Succeed(requirement);
            }

            return Task.CompletedTask;
        }
    }
}
