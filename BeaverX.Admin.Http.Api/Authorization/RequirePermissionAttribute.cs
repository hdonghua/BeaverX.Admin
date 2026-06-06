using Microsoft.AspNetCore.Authorization;

namespace BeaverX.Admin.Http.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = $"{PermissionPolicyProvider.PolicyPrefix}{permission}";
    }
}
