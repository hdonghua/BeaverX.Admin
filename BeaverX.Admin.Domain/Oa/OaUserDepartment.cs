using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

public class OaUserDepartment : Entity<Guid>
{
    protected OaUserDepartment() { }
    public OaUserDepartment(Guid id) => Id = id;
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsPrimary { get; set; } = true;
}
