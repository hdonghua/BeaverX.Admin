using Volo.Abp.Domain.Entities;

namespace BeaverX.Admin.Domain.Oa;

public class OaFormField : Entity<Guid>
{
    protected OaFormField() { }
    public OaFormField(Guid id) => Id = id;
    public Guid DefId { get; set; }
    public string FieldKey { get; set; } = null!;
    public int FieldType { get; set; }
    public string Label { get; set; } = null!;
    public bool IsSummary { get; set; }
    public bool IsRequired { get; set; }
    public string? Placeholder { get; set; }
    public string? Extras { get; set; }
    public int SortOrder { get; set; }
}
