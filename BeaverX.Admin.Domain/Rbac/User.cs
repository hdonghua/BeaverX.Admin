using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class User : FullAuditedEntity
{
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? NickName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsEnabled { get; set; } = true;

    [Navigate(NavigateType.OneToMany, nameof(UserRole.UserId))]
    public List<UserRole> UserRoles { get; set; } = null!;
}
