namespace BeaverX.Admin.Application.Contracts.Rbac.Dtos;

public class OnlineUserDto
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string? NickName { get; set; }
    public int ConnectionCount { get; set; }
    public DateTime ConnectedAt { get; set; }
    public DateTime LastActiveAt { get; set; }
}
