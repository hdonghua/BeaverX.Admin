using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

public class OaConditionGroup : Entity<Guid>
{
    protected OaConditionGroup() { }
    public OaConditionGroup(Guid id) => Id = id;
    public Guid NodeId { get; set; }
    public string? GroupKey { get; set; }
}
