using BeaverX.Domain.Entities;

namespace BeaverX.Admin.Domain.Rbac;

public class UserRefreshToken : FullAuditedEntity
{
    public long UserId { get; set; }
    public string TokenHash { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    [Navigate(NavigateType.OneToOne, nameof(UserId))]
    public User User { get; set; } = null!;
}
