using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

[SugarTable("sys_role_menus")]
public class RoleMenu : Entity
{
    public long RoleId { get; set; }
    public long MenuId { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public Role Role { get; set; } = null!;

    [Navigate(NavigateType.OneToOne, nameof(MenuId))]
    public Menu Menu { get; set; } = null!;
}
