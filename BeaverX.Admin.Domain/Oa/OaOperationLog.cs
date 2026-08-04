using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaOperationLog : CreationAuditedEntity<Guid>
{
    protected OaOperationLog() { }
    public OaOperationLog(Guid id) => Id = id;
    public Guid InstanceId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid Operator { get; set; }
    public OaOperationType OperationType { get; set; }
    public Guid? SourceNodeId { get; set; }
    public Guid? TargetNodeId { get; set; }
    public string? Remark { get; set; }
}

