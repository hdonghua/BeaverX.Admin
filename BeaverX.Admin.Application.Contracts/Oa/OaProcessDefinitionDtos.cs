using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeaverX.Admin.Application.Contracts.Oa;

public class OaFlowGroupQuery
{
    public string? Name { get; set; }
}

public class OaAddProcessGroupRequest
{
    public string Name { get; set; } = null!;
}

public class OaUpdateProcessGroupRequest
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class OaIdRequest
{
    public Guid Id { get; set; }
}

public class OaFlowDefinitionIdRequest
{
    public Guid FlowDefId { get; set; }
}

public class OaCopyProcessRequest : OaFlowDefinitionIdRequest
{
    public string Name { get; set; } = null!;
}

public class OaProcessGroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
}

public class OaFlowGroupDto : OaProcessGroupDto
{
    public List<OaFlowDefinitionDto> FlowDefinitions { get; set; } = [];
}

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
    public List<OaAssigneeRequest>? Transactors { get; set; }
    public int ApprovalType { get; set; }
    public int FlowNodeNoAuditorType { get; set; }
    public int FlowNodeSelfAuditorType { get; set; }
    public string? FlowNodeNoAuditorAssignee { get; set; }
    public string? FlowNodeAuditAdmin { get; set; }
    public int? MultiInstanceApprovalType { get; set; }
    public bool Backable { get; set; }
    public bool Signable { get; set; }
    public bool Assignable { get; set; }
    public bool Signature { get; set; }
    public List<OaFormAuthRequest>? FormAuths { get; set; }
}

public class OaFormAuthRequest
{
    public string Name { get; set; } = null!;
    public int Type { get; set; }
    public string? Label { get; set; }
    public bool? Readable { get; set; }
    public bool? Editable { get; set; }
    public List<OaFormAuthRequest>? Details { get; set; }
}

public class OaAssigneeRequest
{
    public string Rid { get; set; } = null!;
    public List<string>? Assignees { get; set; }
    public List<string>? Roles { get; set; }
    public int AssigneeType { get; set; }
    public int CcType { get; set; }
    public int TransactorType { get; set; }
    public int? LayerType { get; set; }
    public int? Layer { get; set; }
}

public class OaFlowConditionGroupRequest
{
    public string Id { get; set; } = null!;
    public List<OaFlowConditionRequest>? Conditions { get; set; }
}

public class OaFlowConditionRequest
{
    public string Id { get; set; } = null!;
    public List<string> Val { get; set; } = [];
    public string VarName { get; set; } = null!;
    public int Operator { get; set; }
    public List<int>? Operators { get; set; }
}
public class OaWorkflowDefinitionRequest
{
    public Guid? Id { get; set; }
    public string? Icon { get; set; }
    public string Name { get; set; } = null!;
    public Guid GroupId { get; set; }
    public int Cancelable { get; set; }
    public List<string> FlowAdminIds { get; set; } = [];
}

public class OaFlowPermissionRequest
{
    public int Type { get; set; }
    public List<OaFlowInitiatorRequest>? FlowInitiators { get; set; }
}

public class OaFlowInitiatorRequest
{
    public string Id { get; set; } = null!;
    public int Type { get; set; }
}

public class OaFlowWidgetRequest
{
    public string Name { get; set; } = null!;
    public int Type { get; set; }
    public string? Label { get; set; }
    public bool Summary { get; set; }
    public bool Required { get; set; }
    public string? Placeholder { get; set; }
    public object? Details { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraProperties { get; set; }
}

public class OaProcessEditDto
{
    public Guid FlowDefId { get; set; }
    public string FlowDefJson { get; set; } = null!;
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
    public bool Readable { get; set; } = true;
    public bool Editable { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtraProperties { get; set; }
}
