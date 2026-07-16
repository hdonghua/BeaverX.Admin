using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Config;

public class SysConfig : FullAuditedEntity
{
    public string Key { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string Label { get; set; } = null!;
    public string? Group { get; set; }
    public string? Remark { get; set; }
    public int Sort { get; set; }
    public bool IsEnabled { get; set; } = true;

    public SysConfig()
    {
        
    }
}
