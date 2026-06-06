using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class RoleMenu : Entity
{
    public long RoleId { get; set; }
    public long MenuId { get; set; }

    public Role Role { get; set; } = null!;
    public Menu Menu { get; set; } = null!;
}
