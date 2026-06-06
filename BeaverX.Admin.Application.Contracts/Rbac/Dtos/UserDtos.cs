namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class UserDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsEnabled { get; set; }
    public DateTime CreationTime { get; set; }
    public List<long> RoleIds { get; set; } = [];
    public List<string> RoleNames { get; set; } = [];
}

public class UserQueryDto
{
    public string? Keyword { get; set; }
    public bool? IsEnabled { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CreateUserDto
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsEnabled { get; set; } = true;
    public List<long> RoleIds { get; set; } = [];
}

public class UpdateUserDto
{
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool? IsEnabled { get; set; }
}

public class AssignUserRolesDto
{
    public List<long> RoleIds { get; set; } = [];
}

public class ResetPasswordDto
{
    public string NewPassword { get; set; } = null!;
}
