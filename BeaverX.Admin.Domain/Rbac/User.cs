using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class User : FullAuditedEntity<Guid>
{
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string NickName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Avatar { get; set; }
    public bool IsEnabled { get; set; } = true;

    public ICollection<UserRole> UserRoles { get; set; } = [];
}
