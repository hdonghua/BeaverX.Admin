using BeaverX.Admin.Domain.Rbac;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Messages;

public class UserMessage : FullAuditedEntity
{
    public long UserId { get; set; }
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Avatar { get; set; }
    public string Content { get; set; } = null!;
    public int? MessageType { get; set; }
    public bool IsRead { get; set; }

    public User User { get; set; } = null!;
}
