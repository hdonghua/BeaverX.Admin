using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class Role : AuditedEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    [Navigate(NavigateType.OneToMany, nameof(UserRole.RoleId))]
    public ICollection<UserRole> UserRoles { get; set; } = [];

    [Navigate(NavigateType.OneToMany, nameof(RoleMenu.RoleId))]
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
