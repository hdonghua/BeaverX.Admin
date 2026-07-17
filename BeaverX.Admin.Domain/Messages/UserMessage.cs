using BeaverX.Admin.Domain.Rbac;
using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Messages;

[SugarTable("sys_user_messages")]
public class UserMessage : CreationAuditedEntity
{
    public long UserId { get; set; }

    /// <summary>
    /// notice
    /// </summary>
    public string Type { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string? SubTitle { get; set; }
    public string? Avatar { get; set; }
    public string Content { get; set; } = null!;
    public int? MessageType { get; set; }
    public bool IsRead { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public User User { get; set; } = null!;
}
