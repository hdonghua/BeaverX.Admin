using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

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