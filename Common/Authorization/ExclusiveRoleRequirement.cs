using Microsoft.AspNetCore.Authorization;

namespace Common.Authorization
{
    public class ExclusiveRoleRequirement : IAuthorizationRequirement
    {
        public string RequiredRole { get; }

        public ExclusiveRoleRequirement(string requiredRole)
        {
            RequiredRole = requiredRole;
        }
    }
}
