using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Realtime;

public interface IOnlineUserTracker
{
    void AddConnection(long userId, string userName, string? nickName, string connectionId);

    void RemoveConnection(string connectionId);

    int RemoveUserConnections(long userId);

    IReadOnlyList<OnlineUserDto> GetOnlineUsers();

    int GetTotalConnectionCount();
}
