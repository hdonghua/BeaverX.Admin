using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class UserRole : Entity<Guid>
{
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
