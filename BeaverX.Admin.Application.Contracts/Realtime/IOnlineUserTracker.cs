using BeaverX.Admin.Application.Contracts.Rbac.Dtos;

namespace BeaverX.Admin.Application.Contracts.Realtime;

public interface IOnlineUserTracker
{
    void AddConnection(
        Guid userId,
        string userName,
        string? nickName,
        string connectionId,
        string deviceId);

    void TouchConnection(string connectionId);

    void RemoveConnection(string connectionId);

    int RemoveUserConnections(Guid userId);

    IReadOnlyList<OnlineUserDto> GetOnlineUsers();

    int GetTotalConnectionCount();
}
