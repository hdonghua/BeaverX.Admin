using BeaverX.Admin.Domain.Shared.Rbac;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace BeaverX.Admin.Http.Api.Authorization;

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public PermissionAuthorizationHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement)
    {
        var user = context.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return;
        }

        if (UserPermissionHttpContext.IsSuperAdmin(user))
        {
            context.Succeed(requirement);
            return;
        }

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext == null)
        {
            return;
        }

        var permissions = await UserPermissionHttpContext.GetOrLoadPermissionsAsync(
            httpContext,
            httpContext.RequestAborted);

        var satisfied = requirement.Match == PermissionMatchMode.All
            ? requirement.Permissions.All(code =>
                permissions.Contains(code, StringComparer.OrdinalIgnoreCase))
            : requirement.Permissions.Any(code =>
                permissions.Contains(code, StringComparer.OrdinalIgnoreCase));

        if (satisfied)
        {
            context.Succeed(requirement);
        }
    }
}
