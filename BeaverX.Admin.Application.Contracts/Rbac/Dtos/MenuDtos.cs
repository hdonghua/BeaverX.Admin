namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class MenuDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public string? PermissionCode { get; set; }
    public int Sort { get; set; }
    public bool IsVisible { get; set; }
    public bool IsEnabled { get; set; }
    public List<MenuDto> Children { get; set; } = [];
}

public class CreateMenuDto
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
}

public class UpdateMenuDto
{
    public long? ParentId { get; set; }
    public string? Name { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Icon { get; set; }
    public string? PermissionCode { get; set; }
    public int? Sort { get; set; }
    public bool? IsVisible { get; set; }
    public bool? IsEnabled { get; set; }
}
