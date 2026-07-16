using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class UserRole : Entity
{
    public long UserId { get; set; }
    public long RoleId { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public User User { get; set; } = null!;

    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    public Role Role { get; set; } = null!;
}
