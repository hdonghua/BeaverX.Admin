using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

[SugarTable("sys_roles")]
public class Role : AuditedEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    [Navigate(NavigateType.OneToMany, nameof(UserRole.RoleId))]
    public List<UserRole> UserRoles { get; set; } = null!;

    [Navigate(NavigateType.OneToMany, nameof(RoleMenu.RoleId))]
    public List<RoleMenu> RoleMenus { get; set; } = null!;
}
