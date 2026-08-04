using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaProcessGroup : FullAuditedEntity<Guid>
{
    protected OaProcessGroup() { }
    public OaProcessGroup(Guid id) => Id = id;
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public short Status { get; set; } = 1;
}
