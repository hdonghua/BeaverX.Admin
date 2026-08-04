using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaTask : FullAuditedEntity<Guid>
{
    protected OaTask() { }
    public OaTask(Guid id) => Id = id;
    public Guid InstanceId { get; set; }
    public Guid NodeId { get; set; }
    public string NodeName { get; set; } = null!;
    public Guid UserId { get; set; }
    public OaTaskStatus Status { get; set; } = OaTaskStatus.Pending;
    public OaOperationType? FlowCmd { get; set; }
    public List<string>? CandidateUsers { get; set; }
    public Guid? ParentTaskId { get; set; }
    public int? LoopCounter { get; set; }
    public DateTime? CompleteTime { get; set; }
    public string? Remark { get; set; }
}
