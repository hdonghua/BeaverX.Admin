using Microsoft.AspNetCore.Authorization;

namespace BeaverX.Admin.Http.Api.Authorization;

/// <summary>
/// 声明接口所需权限。传入多个权限码时默认满足其一（Any / some）即可访问。
/// 同一方法上叠加多个特性时为 AND 关系，需全部满足。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
    {
        Policy = PermissionPolicyProvider.BuildPolicyName([permission], PermissionMatchMode.All);
    }

    public RequirePermissionAttribute(string permission, string permission2, params string[] morePermissions)
    {
        var permissions = new List<string> { permission, permission2 };
        if (morePermissions.Length > 0)
        {
            permissions.AddRange(morePermissions);
        }

        Policy = PermissionPolicyProvider.BuildPolicyName(permissions, PermissionMatchMode.Any);
    }

    public RequirePermissionAttribute(PermissionMatchMode match, params string[] permissions)
    {
        ArgumentNullException.ThrowIfNull(permissions);
        if (permissions.Length == 0)
        {
            throw new ArgumentException("At least one permission code is required.", nameof(permissions));
        }

        Policy = PermissionPolicyProvider.BuildPolicyName(permissions, match);
    }
}
