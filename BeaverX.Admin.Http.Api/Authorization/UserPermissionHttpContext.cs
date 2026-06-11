using System.Security.Claims;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Http.Api.Authorization;

public static class UserPermissionHttpContext
{
    public const string ItemsKey = "rbac:user-permissions";

    public static bool IsSuperAdmin(ClaimsPrincipal user) =>
        user.IsInRole(RbacPermissionCodes.SuperAdmin);

    public static async Task<IReadOnlyCollection<string>> GetOrLoadPermissionsAsync(
        HttpContext? httpContext,
        CancellationToken cancellationToken = default)
    {
        if (httpContext == null)
        {
            return [];
        }

        if (httpContext.Items.TryGetValue(ItemsKey, out var existing) &&
            existing is IReadOnlyCollection<string> cached)
        {
            return cached;
        }

        if (!long.TryParse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return [];
        }

        var resolver = httpContext.RequestServices.GetRequiredService<IUserPermissionResolver>();
        var permissions = await resolver.GetPermissionsAsync(userId, cancellationToken);
        httpContext.Items[ItemsKey] = permissions;
        return permissions;
    }
}
