using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class Menu : FullAuditedEntity
{
    public long? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public string? PermissionCode { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;

    public Menu? Parent { get; set; }
    public ICollection<Menu> Children { get; set; } = [];
    public ICollection<RoleMenu> RoleMenus { get; set; } = [];
}
