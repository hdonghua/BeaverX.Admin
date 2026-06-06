using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class UserRole : Entity
{
    public long UserId { get; set; }
    public long RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
