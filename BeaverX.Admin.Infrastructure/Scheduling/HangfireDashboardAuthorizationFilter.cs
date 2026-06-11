using System.Security.Claims;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using Hangfire.Dashboard;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure.Scheduling;

public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            return false;
        }

        if (user.IsInRole(RbacPermissionCodes.SuperAdmin))
        {
            return true;
        }

        if (!long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return false;
        }

        var resolver = httpContext.RequestServices.GetRequiredService<IUserPermissionResolver>();
        var permissions = resolver.GetPermissionsAsync(userId).GetAwaiter().GetResult();
        return permissions.Contains(RbacPermissionCodes.System.Job.List);
    }
}
