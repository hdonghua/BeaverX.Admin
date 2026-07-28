using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class RoleMenu : Entity<Guid>
{
    public Guid RoleId { get; set; }
    public Guid MenuId { get; set; }

    public Role Role { get; set; } = null!;
    public Menu Menu { get; set; } = null!;
}
