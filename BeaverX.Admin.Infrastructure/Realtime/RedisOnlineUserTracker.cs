using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BeaverX.Admin.Infrastructure.Realtime;

/// <summary>
/// 基于 Redis Hash 的集群在线用户追踪（多节点共享）。
/// 默认未启用；见 <see cref="RealtimeDistributedExtensions.AddRedisOnlineUserTracker"/>。
/// </summary>
public class RedisOnlineUserTracker : IOnlineUserTracker
{
    private const string ConnectionsHashSuffix = "online:connections";

    private readonly IDatabase _database;
    private readonly string _connectionsHashKey;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public RedisOnlineUserTracker(IDatabase database, IOptions<CacheOptions> cacheOptions)
    {
        _database = database;
        var prefix = cacheOptions.Value.KeyPrefix?.Trim() ?? "beaverx:admin:";
        if (!prefix.EndsWith(':'))
        {
            prefix += ":";
        }

        _connectionsHashKey = prefix + ConnectionsHashSuffix;
    }

    public void AddConnection(long userId, string userName, string? nickName, string connectionId)
    {
        var record = new ConnectionRecord
        {
            UserId = userId,
            UserName = userName,
            NickName = nickName,
            ConnectionId = connectionId,
            ConnectedAt = DateTime.UtcNow
        };

        _database.HashSet(
            _connectionsHashKey,
            connectionId,
            JsonSerializer.Serialize(record, JsonOptions));
    }

    public void RemoveConnection(string connectionId)
    {
        _database.HashDelete(_connectionsHashKey, connectionId);
    }

    public int RemoveUserConnections(long userId)
    {
        var entries = _database.HashGetAll(_connectionsHashKey);
        if (entries.Length == 0)
        {
            return 0;
        }

        var connectionIds = new List<RedisValue>();
        foreach (var entry in entries)
        {
            var record = Deserialize(entry.Value);
            if (record != null && record.UserId == userId)
            {
                connectionIds.Add(entry.Name);
            }
        }

        if (connectionIds.Count == 0)
        {
            return 0;
        }

        _database.HashDelete(_connectionsHashKey, connectionIds.ToArray());
        return connectionIds.Count;
    }

    public IReadOnlyList<OnlineUserDto> GetOnlineUsers()
    {
        var entries = _database.HashGetAll(_connectionsHashKey);
        if (entries.Length == 0)
        {
            return [];
        }

        var records = entries
            .Select(x => Deserialize(x.Value))
            .Where(x => x != null)
            .Cast<ConnectionRecord>()
            .ToList();

        return records
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

    public int GetTotalConnectionCount()
    {
        return (int)_database.HashLength(_connectionsHashKey);
    }

    private static ConnectionRecord? Deserialize(RedisValue value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ConnectionRecord>(value.ToString(), JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private sealed class ConnectionRecord
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string? NickName { get; set; }
        public string ConnectionId { get; set; } = null!;
        public DateTime ConnectedAt { get; set; }
    }
}
