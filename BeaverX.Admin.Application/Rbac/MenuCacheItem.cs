using BeaverX.Admin.Domain.Rbac;
using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Rbac;

/// <summary>
/// 菜单缓存快照（无导航属性，避免 JSON 循环引用）。
/// </summary>
internal sealed class MenuCacheItem
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public MenuType MenuType { get; set; }
    public string? Perms { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public bool IsExternal { get; set; }

    public static MenuCacheItem FromEntity(Menu menu) => new()
    {
        Id = menu.Id,
        ParentId = menu.ParentId,
        Name = menu.Name,
        MenuType = menu.MenuType,
        Perms = menu.Perms,
        Path = menu.Path,
        Component = menu.Component,
        Icon = menu.Icon,
        Sort = menu.Sort,
        IsVisible = menu.IsVisible,
        IsEnabled = menu.IsEnabled,
        IsExternal = menu.IsExternal
    };

    public Menu ToEntity() => new()
    {
        Id = Id,
        ParentId = ParentId,
        Name = Name,
        MenuType = MenuType,
        Perms = Perms,
        Path = Path,
        Component = Component,
        Icon = Icon,
        Sort = Sort,
        IsVisible = IsVisible,
        IsEnabled = IsEnabled,
        IsExternal = IsExternal
    };
}
