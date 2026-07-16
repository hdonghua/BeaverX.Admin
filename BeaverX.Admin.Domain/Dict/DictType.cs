using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Dict;

public class DictType : FullAuditedEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Remark { get; set; }
    public bool IsEnabled { get; set; } = true;

    // 集合属性名与类型名 DictData 冲突，不能用 nameof(DictData.DictTypeId)
    [Navigate(NavigateType.OneToMany, "DictTypeId")]
    public ICollection<DictData> DictData { get; set; } = [];
}
