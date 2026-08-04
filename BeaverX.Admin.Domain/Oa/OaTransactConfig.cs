using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

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