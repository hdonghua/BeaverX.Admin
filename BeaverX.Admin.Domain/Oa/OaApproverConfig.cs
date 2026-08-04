using BeaverX.Admin.Domain.Shared.Oa;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

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
