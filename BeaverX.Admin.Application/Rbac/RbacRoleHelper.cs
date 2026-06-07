using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Rbac;

internal static class RbacRoleHelper
{
    public static bool IsSuperAdminRole(string? code) =>
        string.Equals(code, RbacPermissionCodes.SuperAdmin, StringComparison.OrdinalIgnoreCase);
}
