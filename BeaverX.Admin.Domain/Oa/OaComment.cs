using Volo.Abp.Domain.Entities.Auditing;

namespace BeaverX.Admin.Domain.Oa;

public class OaComment : CreationAuditedEntity<Guid>
{
    protected OaComment() { }
    public OaComment(Guid id) => Id = id;
    public Guid InstanceId { get; set; }
    public Guid? TaskId { get; set; }
    public Guid Commenter { get; set; }
    public string Content { get; set; } = null!;
    public string? Attachment { get; set; }
}
