using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaDepartment : AuditedEntity<Guid>
{
    protected OaDepartment() { }
    public OaDepartment(Guid id) => Id = id;
    public Guid? ParentId { get; set; }
    public string Name { get; set; } = null!;
    public string? Code { get; set; }
    public Guid? LeaderUserId { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
}
