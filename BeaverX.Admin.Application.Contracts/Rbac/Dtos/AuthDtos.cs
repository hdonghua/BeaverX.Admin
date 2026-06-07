namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class LoginDto
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
}

public class LoginResultDto
{
    public string Token { get; set; } = null!;
    public int ExpiresIn { get; set; }
    public UserProfileDto User { get; set; } = null!;
}

public class UserProfileDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = null!;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public List<string> Roles { get; set; } = [];
    public List<string> Permissions { get; set; } = [];
}

public class UpdateProfileDto
{
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
}

public class ChangePasswordDto
{
    public string OldPassword { get; set; } = null!;
    public string NewPassword { get; set; } = null!;
}
