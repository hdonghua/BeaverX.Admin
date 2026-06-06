using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class Permission : FullAuditedEntity
{
    public long? ParentId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PermissionType Type { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    public Permission? Parent { get; set; }
    public ICollection<Permission> Children { get; set; } = [];
    public ICollection<RolePermission> RolePermissions { get; set; } = [];
}
