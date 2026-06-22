using System.Collections.Concurrent;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;

namespace BeaverX.Admin.Infrastructure.Realtime;

public class OnlineUserTracker : IOnlineUserTracker
{
    private sealed class ConnectionEntry
    {
        public long UserId { get; init; }
        public string UserName { get; init; } = null!;
        public string? NickName { get; init; }
        public string ConnectionId { get; init; } = null!;
        public DateTime ConnectedAt { get; init; }
    }

    private readonly ConcurrentDictionary<string, ConnectionEntry> _connections = new();

    public void AddConnection(long userId, string userName, string? nickName, string connectionId)
    {
        _connections[connectionId] = new ConnectionEntry
        {
            UserId = userId,
            UserName = userName,
            NickName = nickName,
            ConnectionId = connectionId,
            ConnectedAt = DateTime.UtcNow
        };
    }

    public void RemoveConnection(string connectionId)
    {
        _connections.TryRemove(connectionId, out _);
    }

    public int RemoveUserConnections(long userId)
    {
        var connectionIds = _connections
            .Where(x => x.Value.UserId == userId)
            .Select(x => x.Key)
            .ToList();

        foreach (var connectionId in connectionIds)
        {
            _connections.TryRemove(connectionId, out _);
        }

        return connectionIds.Count;
    }

    public IReadOnlyList<OnlineUserDto> GetOnlineUsers()
    {
        return _connections.Values
            .GroupBy(x => x.UserId)
            .Select(group =>
            {
                var first = group.OrderBy(x => x.ConnectedAt).First();
                var last = group.OrderByDescending(x => x.ConnectedAt).First();

                return new OnlineUserDto
                {
                    UserId = first.UserId,
                    UserName = first.UserName,
                    NickName = first.NickName,
                    ConnectionCount = group.Count(),
                    ConnectedAt = first.ConnectedAt,
                    LastActiveAt = last.ConnectedAt
                };
            })
            .OrderByDescending(x => x.LastActiveAt)
            .ToList();
    }

    public int GetTotalConnectionCount() => _connections.Count;
}
