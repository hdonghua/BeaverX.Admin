using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Oa;

public class OaFlowGroupQuery { public string? Name { get; set; } }
public class OaAddProcessGroupRequest { public string Name { get; set; } = null!; }
public class OaUpdateProcessGroupRequest { public Guid Id { get; set; } public string Name { get; set; } = null!; }
public class OaIdRequest { public Guid Id { get; set; } }
public class OaFlowDefinitionIdRequest { public Guid FlowDefId { get; set; } }
public class OaCopyProcessRequest : OaFlowDefinitionIdRequest { public string Name { get; set; } = null!; }
public class OaProcessGroupDto { public Guid Id { get; set; } public string Name { get; set; } = null!; }
public class OaFlowGroupDto : OaProcessGroupDto { public List<OaFlowDefinitionDto> FlowDefinitions { get; set; } = []; }

public class OaFlowDefinitionDto
{
    public Guid Id { get; set; }
    public string LinkId { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Icon { get; set; }
    public int InitiatorType { get; set; }
    public int Version { get; set; }
    public Guid GroupId { get; set; }
    public string? Remark { get; set; }
    public bool Cancelable { get; set; }
    public int Status { get; set; }
    public bool ShowInWorkbench { get; set; } = true;
    public bool Editable { get; set; }
    public List<OaFlowInitiatorRequest> FlowInitiators { get; set; } = [];
}

public class OaAddProcessRequest
{
    public OaFlowNodeRequest NodeConfig { get; set; } = null!;
    public List<OaFlowWidgetRequest> FlowWidgets { get; set; } = [];
    public OaWorkflowDefinitionRequest WorkFlowDef { get; set; } = null!;
    public OaFlowPermissionRequest FlowPermission { get; set; } = null!;
    public string FlowDefJson { get; set; } = null!;
}

public class OaFlowNodeRequest
{
    public string Name { get; set; } = null!;
    public int Type { get; set; }
    public OaFlowNodeRequest? ChildNode { get; set; }
    public List<OaFlowNodeRequest>? ConditionNodes { get; set; }
    public List<OaFlowConditionGroupRequest>? ConditionGroups { get; set; }
    public int PriorityLevel { get; set; }
    public string? ConditionExpression { get; set; }
    public List<OaAssigneeRequest>? Assignees { get; set; }
    public List<OaAssigneeRequest>? Ccs { get; set; }
    public int ApprovalType { get; set; }
    public int FlowNodeNoAuditorType { get; set; }
    public int FlowNodeSelfAuditorType { get; set; }
    public string? FlowNodeNoAuditorAssignee { get; set; }
    public int? MultiInstanceApprovalType { get; set; }
    public bool Backable { get; set; }
    public bool Signable { get; set; }
    public bool Assignable { get; set; }
    public bool Signature { get; set; }
}

public class OaAssigneeRequest
{
    public string Rid { get; set; } = null!;
    public List<string>? Assignees { get; set; }
    public List<string>? Roles { get; set; }
    public int AssigneeType { get; set; }
    public int CcType { get; set; }
    public int? LayerType { get; set; }
    public int? Layer { get; set; }
}

public class OaFlowConditionGroupRequest { public string Id { get; set; } = null!; public List<OaFlowConditionRequest>? Conditions { get; set; } }
public class OaFlowConditionRequest { public string Id { get; set; } = null!; public List<string> Val { get; set; } = []; public string VarName { get; set; } = null!; public int Operator { get; set; } public List<int>? Operators { get; set; } }
public class OaWorkflowDefinitionRequest { public Guid? Id { get; set; } public string? Icon { get; set; } public string Name { get; set; } = null!; public Guid GroupId { get; set; } public int Cancelable { get; set; } public List<string> FlowAdminIds { get; set; } = []; }
public class OaFlowPermissionRequest { public int Type { get; set; } public List<OaFlowInitiatorRequest>? FlowInitiators { get; set; } }
public class OaFlowInitiatorRequest { public string Id { get; set; } = null!; public int Type { get; set; } }
public class OaFlowWidgetRequest { public string Name { get; set; } = null!; public int Type { get; set; } public string? Label { get; set; } public bool Summary { get; set; } public bool Required { get; set; } public string? Placeholder { get; set; } public object? Details { get; set; } }
public class OaProcessEditDto { public Guid FlowDefId { get; set; } public string FlowDefJson { get; set; } = null!; }

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

public class OaFlowFormFieldDto
{
    public Guid Id { get; set; }
    public Guid FlowDefId { get; set; }
    public string Name { get; set; } = null!;
    public string? Label { get; set; }
    public string? Placeholder { get; set; }
    public int Type { get; set; }
    public bool Required { get; set; }
    public bool Summary { get; set; }
    public bool Locale { get; set; }
    public bool Comma { get; set; }
    public string? Format { get; set; }
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

public class OaLaunchRequest { public Guid FlowDefId { get; set; } public string FlowValue { get; set; } = "{}"; public Dictionary<Guid, List<string>>? Designees { get; set; } }
public class OaTaskActionRequest { public Guid FlowInstId { get; set; } public Guid TaskId { get; set; } public int FlowCmd { get; set; } public List<string>? FileIds { get; set; } public string? Comment { get; set; } public string? Assignee { get; set; } public string? UserId { get; set; } public Guid? TargetNodeId { get; set; } }
public class OaCommentRequest { public Guid InstanceId { get; set; } public Guid? TaskId { get; set; } public string Content { get; set; } = null!; public string? Attachment { get; set; } }
public class OaFormModifyRequest { public Guid FlowInstId { get; set; } public Guid FlowNodeId { get; set; } public Guid TaskId { get; set; } public string FlowValue { get; set; } = "{}"; }
public class OaFlowInstanceIdRequest { public Guid FlowInstId { get; set; } }
public class OaFormEditRecordDto { public string CreatorId { get; set; } = null!; public DateTime CreateTime { get; set; } public string FormValue { get; set; } = "{}"; }
public class OaTransferRequest { public Guid FlowInstId { get; set; } public string? AssigneeId { get; set; } public string? NewAssigneeId { get; set; } public string? Comment { get; set; } }

public class OaOrganizationOptionsDto { public List<OaDepartmentOptionDto> Depts { get; set; } = []; public List<OaRoleOptionDto> Roles { get; set; } = []; public List<OaUserOptionDto> Users { get; set; } = []; }
public class OaDepartmentOptionDto { public Guid Id { get; set; } public string Name { get; set; } = null!; public Guid? ParentId { get; set; } public string? Code { get; set; } public List<OaDepartmentOptionDto> Children { get; set; } = []; }
public class OaRoleOptionDto { public string Id { get; set; } = null!; public string Name { get; set; } = null!; public string? Description { get; set; } }
public class OaUserOptionDto { public string Id { get; set; } = null!; public string Name { get; set; } = null!; public string? UserName { get; set; } public string? Avatar { get; set; } }
