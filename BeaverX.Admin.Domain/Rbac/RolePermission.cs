using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class RolePermission : Entity
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
