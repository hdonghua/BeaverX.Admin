using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

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
