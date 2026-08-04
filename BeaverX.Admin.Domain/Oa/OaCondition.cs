using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

public class OaCondition : Entity<Guid>
{
    protected OaCondition() { }
    public OaCondition(Guid id) => Id = id;
    public Guid GroupId { get; set; }
    public string VarName { get; set; } = null!;
    public int Operator { get; set; }
    public List<string>? Values { get; set; }
    public List<int>? Operators { get; set; }
}
