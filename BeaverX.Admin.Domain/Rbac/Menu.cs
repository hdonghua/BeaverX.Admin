using BeaverX.Admin.Domain.Shared.Rbac;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class Menu : FullAuditedEntity
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public MenuType MenuType { get; set; }
    /// <summary>权限标识</summary>
    public string? Perms { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsExternal { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(ParentId))]
    public Menu? Parent { get; set; }

    [Navigate(NavigateType.OneToMany, nameof(ParentId))]
    public ICollection<Menu> Children { get; set; } = [];

    [Navigate(NavigateType.OneToMany, nameof(RoleMenu.MenuId))]
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
