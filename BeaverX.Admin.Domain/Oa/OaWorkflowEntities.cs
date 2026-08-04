using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities;
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

public class OaUserDepartment : Entity<Guid>
{
    protected OaUserDepartment() { }
    public OaUserDepartment(Guid id) => Id = id;
    public Guid UserId { get; set; }
    public Guid DepartmentId { get; set; }
    public bool IsPrimary { get; set; } = true;
}

public class OaProcessGroup : FullAuditedEntity<Guid>
{
    protected OaProcessGroup() { }
    public OaProcessGroup(Guid id) => Id = id;
    public string Name { get; set; } = null!;
    public int SortOrder { get; set; }
    public string? Description { get; set; }
    public short Status { get; set; } = 1;
}

public class OaProcessDefinition : FullAuditedEntity<Guid>
{
    protected OaProcessDefinition() { }
    public OaProcessDefinition(Guid id) => Id = id;
    public OaPermissionType PermissionType { get; set; }
    public string BelongKey { get; set; } = null!;
    public int Version { get; set; }
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }
    public Guid GroupId { get; set; }
    public bool Cancelable { get; set; } = true;
    public List<string> FlowAdminIds { get; set; } = [];
    public OaDefinitionStatus Status { get; set; } = OaDefinitionStatus.Draft;
    public string DefJson { get; set; } = null!;
}

public class OaFormField : Entity<Guid>
{
    protected OaFormField() { }
    public OaFormField(Guid id) => Id = id;
    public Guid DefId { get; set; }
    public string FieldKey { get; set; } = null!;
    public int FieldType { get; set; }
    public string Label { get; set; } = null!;
    public bool IsSummary { get; set; }
    public bool IsRequired { get; set; }
    public string? Placeholder { get; set; }
    public string? Extras { get; set; }
    public int SortOrder { get; set; }
}

public class OaInitiator : Entity<Guid>
{
    protected OaInitiator() { }
    public OaInitiator(Guid id) => Id = id;
    public Guid DefId { get; set; }
    public int InitiatorType { get; set; }
    public List<string> InitiatorIds { get; set; } = [];
}

public class OaNode : Entity<Guid>
{
    protected OaNode() { }
    public OaNode(Guid id) => Id = id;
    public Guid DefId { get; set; }
    public string NodeName { get; set; } = null!;
    public OaNodeType NodeType { get; set; }
    public Guid? ParentNodeId { get; set; }
    public bool IsConditionBranch { get; set; }
    public int? PriorityLevel { get; set; }
    public string? ConditionExpression { get; set; }
    public Guid? ChildNodeId { get; set; }
    public int ApprovalType { get; set; }
    public int? MultiInstanceApprovalType { get; set; }
    public int? FlowNodeNoAuditorType { get; set; }
    public string? FlowNodeNoAuditorAssignee { get; set; }
    public int? FlowNodeSelfAuditorType { get; set; }
    public string? Extras { get; set; }
    public bool Backable { get; set; }
    public bool Signable { get; set; }
    public bool Assignable { get; set; }
    public bool Signature { get; set; }
}

public class OaConditionGroup : Entity<Guid>
{
    protected OaConditionGroup() { }
    public OaConditionGroup(Guid id) => Id = id;
    public Guid NodeId { get; set; }
    public string? GroupKey { get; set; }
}

public class OaCondition : Entity<Guid>
{
    protected OaCondition() { }
    public OaCondition(Guid id) => Id = id;
    public Guid GroupId { get; set; }
    public string VarName { get; set; } = null!;
    public int Operator { get; set; }
    public List<string>? Values { get; set; }
    public List<int>? Operators { get; set; }
}

public class OaApproverConfig : Entity<Guid>
{
    protected OaApproverConfig() { }
    public OaApproverConfig(Guid id) => Id = id;
    public Guid NodeId { get; set; }
    public string Rid { get; set; } = null!;
    public OaAssigneeType AssigneeType { get; set; }
    public int? LayerType { get; set; }
    public int? Layer { get; set; }
    public List<string> Assignees { get; set; } = [];
    public List<string> Roles { get; set; } = [];
}

public class OaCcConfig : Entity<Guid>
{
    protected OaCcConfig() { }
    public OaCcConfig(Guid id) => Id = id;
    public Guid NodeId { get; set; }
    public string Rid { get; set; } = null!;
    public int CcType { get; set; }
    public List<string> Assignees { get; set; } = [];
    public List<string> Roles { get; set; } = [];
    public int? LayerType { get; set; }
    public int? Layer { get; set; }
}

public class OaTransactConfig : Entity<Guid>
{
    protected OaTransactConfig() { }
    public OaTransactConfig(Guid id) => Id = id;
    public Guid NodeId { get; set; }
    public string Rid { get; set; } = null!;
    public int AssigneeType { get; set; }
    public List<string> Assignees { get; set; } = [];
    public List<string> Roles { get; set; } = [];
}

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

public class OaComment : CreationAuditedEntity<Guid>
{
    protected OaComment() { }
    public OaComment(Guid id) => Id = id;
    public Guid InstanceId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid Commenter { get; set; }
    public string Content { get; set; } = null!;
    public string? Attachment { get; set; }
}

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
