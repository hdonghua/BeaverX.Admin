using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Dict;

public class DictData : FullAuditedEntity
{
    public long DictTypeId { get; set; }
    public string Label { get; set; } = null!;
    public string Value { get; set; } = null!;
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;
    public string? CssClass { get; set; }
    public string? ListClass { get; set; }
    public string? Remark { get; set; }

    public DictType DictType { get; set; } = null!;
}
