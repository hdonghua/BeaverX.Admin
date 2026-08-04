using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaCcRecord : CreationAuditedEntity<Guid>
{
    protected OaCcRecord() { }
    public OaCcRecord(Guid id) => Id = id;
    public Guid InstanceId { get; set; }
    public Guid NodeId { get; set; }
    public Guid UserId { get; set; }
    public bool IsRead { get; set; }
    public DateTime? ReadTime { get; set; }
}
