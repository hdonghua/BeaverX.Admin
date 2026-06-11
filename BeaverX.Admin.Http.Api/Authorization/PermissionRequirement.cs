using Microsoft.AspNetCore.Authorization;

namespace BeaverX.Admin.Http.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
        : this([permission], PermissionMatchMode.All)
    {
    }

    public PermissionRequirement(IReadOnlyList<string> permissions, PermissionMatchMode match)
    {
        if (permissions.Count == 0)
        {
            throw new ArgumentException("At least one permission code is required.", nameof(permissions));
        }

        Permissions = permissions;
        Match = match;
    }

    public IReadOnlyList<string> Permissions { get; }

    public PermissionMatchMode Match { get; }
}
