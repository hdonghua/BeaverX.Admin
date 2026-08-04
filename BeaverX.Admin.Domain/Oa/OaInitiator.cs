using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

public class OaInitiator : Entity<Guid>
{
    protected OaInitiator() { }
    public OaInitiator(Guid id) => Id = id;
    public Guid DefId { get; set; }
    public int InitiatorType { get; set; }
    public List<string> InitiatorIds { get; set; } = [];
}