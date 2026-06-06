using BeaverX.Admin.Domain.Shared.Rbac;

namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class PermissionDto
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PermissionType Type { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; }
    public List<PermissionDto> Children { get; set; } = [];
}

public class CreatePermissionDto
{
    public long? ParentId { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public PermissionType Type { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}

public class UpdatePermissionDto
{
    public long? ParentId { get; set; }
    public string? Name { get; set; }
    public PermissionType? Type { get; set; }
    public string? Path { get; set; }
    public string? Method { get; set; }
    public int? Sort { get; set; }
    public bool? IsEnabled { get; set; }
}
