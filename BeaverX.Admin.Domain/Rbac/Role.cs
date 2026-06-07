using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class Role : FullAuditedEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
