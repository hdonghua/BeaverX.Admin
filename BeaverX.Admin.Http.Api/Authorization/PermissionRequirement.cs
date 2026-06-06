using Microsoft.AspNetCore.Authorization;

namespace BeaverX.Admin.Http.Api.Authorization;

public class PermissionRequirement : IAuthorizationRequirement
{
    public PermissionRequirement(string permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
