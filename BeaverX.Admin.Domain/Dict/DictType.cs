using Volo.Abp.Domain.Entities.Auditing;
using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Dict;

public class DictType : FullAuditedEntity<Guid>
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Remark { get; set; }
    public bool IsEnabled { get; set; } = true;

    public ICollection<DictData> DictData { get; set; } = [];
}
