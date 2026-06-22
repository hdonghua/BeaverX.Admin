using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Realtime.Dtos;

public class OnlineUsersChangedPayload
{
    public List<OnlineUserDto> Users { get; set; } = [];
    public int TotalConnections { get; set; }
}
