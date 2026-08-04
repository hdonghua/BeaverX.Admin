using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaInstance : FullAuditedEntity<Guid>
{
    protected OaInstance() { }
    public OaInstance(Guid id) => Id = id;
    public Guid DefId { get; set; }
    public Guid Initiator { get; set; }
    public string FormValue { get; set; } = "{}";
    public OaInstanceStatus Status { get; set; } = OaInstanceStatus.Underway;
    public DateTime? EndTime { get; set; }
}
