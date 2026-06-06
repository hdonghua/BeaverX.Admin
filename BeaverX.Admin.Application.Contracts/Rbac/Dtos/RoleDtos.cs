namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class RoleDto
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreationTime { get; set; }
    public List<long> PermissionIds { get; set; } = [];
    public List<long> MenuIds { get; set; } = [];
}

public class RoleQueryDto
{
    public string? Keyword { get; set; }
    public bool? IsEnabled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateRoleDto
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdateRoleDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? Sort { get; set; }
    public bool? IsEnabled { get; set; }
}

public class AssignRolePermissionsDto
{
    public List<long> PermissionIds { get; set; } = [];
}

public class AssignRoleMenusDto
{
    public List<long> MenuIds { get; set; } = [];
}
