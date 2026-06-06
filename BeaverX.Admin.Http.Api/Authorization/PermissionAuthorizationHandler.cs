using BeaverX.Admin.Domain.Shared.Rbac;
using Microsoft.AspNetCore.Authorization;

namespace BeaverX.Admin.Http.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        if (user.IsInRole(RbacPermissionCodes.SuperAdmin))
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (user.HasClaim(RbacClaimTypes.Permission, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
