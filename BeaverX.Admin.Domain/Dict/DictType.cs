using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Dict;

[SugarTable("sys_dict_types")]
public class DictType : FullAuditedEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Remark { get; set; }
    public bool IsEnabled { get; set; } = true;

    [Navigate(NavigateType.OneToMany, "DictTypeId")]
    public List<DictData> DictData { get; set; } = null!;
}
