using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class MenuDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
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
    public bool IsCache { get; set; }
    public List<MenuDto>? Children { get; set; }
}

public class CreateMenuDto
{
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public MenuType MenuType { get; set; }
    public string? Perms { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; } = true;
    public bool IsEnabled { get; set; } = true;
    public bool IsExternal { get; set; }
    public bool IsCache { get; set; } = true;
}

public class UpdateMenuDto
{
    public Guid? ParentId { get; set; }
    public string? Name { get; set; }
    public MenuType? MenuType { get; set; }
    public string? Perms { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public int? Sort { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsEnabled { get; set; }
    public bool? IsExternal { get; set; }
    public bool? IsCache { get; set; }
}

public class ReorderMenusDto
{
    public Guid? ParentId { get; set; }
    public List<Guid> OrderedIds { get; set; } = [];
}
