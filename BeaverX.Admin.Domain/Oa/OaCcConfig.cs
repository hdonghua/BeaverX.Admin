using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

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
