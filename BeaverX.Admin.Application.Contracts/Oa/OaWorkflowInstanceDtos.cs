using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Oa;

public class OaFlowInstanceQuery : PagedQueryDto
{
    public int Current { get => Page; set => Page = value; }
    public string? Keyword { get; set; }
    public int? Status { get; set; }
    public DateTime? BeginMinTime { get; set; }
    public DateTime? BeginMaxTime { get; set; }
    public DateTime? EndMinTime { get; set; }
    public DateTime? EndMaxTime { get; set; }
}

public class OaFlowInstanceListDto
{
    public Guid FlowDefId { get; set; }
    public string Name { get; set; } = null!;
    public Guid GroupId { get; set; }
    public bool Cancelable { get; set; }
    public Guid Id { get; set; }
    public string InstanceNo { get; set; } = null!;
    public string InitiatorId { get; set; } = null!;
    public DateTime? BeginTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Status { get; set; }
    public Guid? TaskId { get; set; }
    public Guid? ActNodeId { get; set; }
    public int NodeSignType { get; set; }
    public bool Assignable { get; set; }
    public bool Signable { get; set; }
    public bool Backable { get; set; }
    public bool Signature { get; set; }
    public int NodeType { get; set; }
    public string? Summary { get; set; }
}

public class OaFlowChartNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid NodeId { get; set; }
    public int NodeType { get; set; }
    public int ApprovalType { get; set; }
    public int MultiInstanceApprovalType { get; set; }
    public int FlowNodeNoAuditorType { get; set; }
    public List<string> UserIds { get; set; } = [];
    public List<string> RoleIds { get; set; } = [];
    public bool InitatorChoice { get; set; }
}

public class OaFlowInstanceDetailsDto
{
    public string FormValue { get; set; } = "{}";
    public List<OaFlowFormFieldDto> FormWidgets { get; set; } = [];
    public List<OaFlowInstanceNodeDto> FutureNodes { get; set; } = [];
    public List<OaFlowInstanceNodeDto> Nodes { get; set; } = [];
}

public class OaFlowInstanceNodeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid FlowInstId { get; set; }
    public Guid FlowNodeId { get; set; }
    public string FlowNodeName { get; set; } = null!;
    public List<string> UserIds { get; set; } = [];
    public List<string> RoleIds { get; set; } = [];
    public bool Underway { get; set; }
    public int Type { get; set; }
    public int NodeType { get; set; }
    public int MultiInstanceApprovalType { get; set; }
    public int? FlowCmd { get; set; }
    public DateTime? AuditTime { get; set; }
    public string? Auditor { get; set; }
    public string? Assignee { get; set; }
    public string? Comment { get; set; }
    public List<object> Files { get; set; } = [];
}

public class OaLaunchRequest
{
    public Guid FlowDefId { get; set; }
    public string FlowValue { get; set; } = "{}";
    public Dictionary<Guid, List<string>>? Designees { get; set; }
}

public class OaTaskActionRequest
{
    public Guid FlowInstId { get; set; }
    public Guid TaskId { get; set; }
    public int FlowCmd { get; set; }
    public List<string>? FileIds { get; set; }
    public string? Comment { get; set; }
    public string? Assignee { get; set; }
    public string? UserId { get; set; }
    public Guid? TargetNodeId { get; set; }
}

public class OaCommentRequest
{
    public Guid InstanceId { get; set; }
    public Guid? TaskId { get; set; }
    public string Content { get; set; } = null!;
    public string? Attachment { get; set; }
}

public class OaFormModifyRequest
{
    public Guid FlowInstId { get; set; }
    public Guid FlowNodeId { get; set; }
    public Guid TaskId { get; set; }
    public string FlowValue { get; set; } = "{}";
}

public class OaFlowInstanceIdRequest
{
    public Guid FlowInstId { get; set; }
}

public class OaFormEditRecordDto
{
    public string CreatorId { get; set; } = null!;
    public DateTime CreateTime { get; set; }
    public string FormValue { get; set; } = "{}";
}

public class OaTransferRequest
{
    public Guid FlowInstId { get; set; }
    public string? AssigneeId { get; set; }
    public string? NewAssigneeId { get; set; }
    public string? Comment { get; set; }
}
