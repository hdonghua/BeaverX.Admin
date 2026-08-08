using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac.Dtos;
using BeaverX.Admin.Application.Contracts.Realtime;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace BeaverX.Admin.Infrastructure.Realtime;

/// <summary>
/// 基于 Redis 的在线设备追踪：按浏览器设备指纹聚合，带 TTL，依赖心跳续期。
/// </summary>
public class RedisOnlineUserTracker : IOnlineUserTracker
{
    /// <summary>无心跳后视为离线的秒数。</summary>
    public const int PresenceTtlSeconds = 90;

    private readonly IDatabase _database;
    private readonly string _keyPrefix;
    private readonly string _deviceIndexKey;

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

        _keyPrefix = prefix;
        _deviceIndexKey = prefix + "online:device-index";
    }

    public void AddConnection(
        Guid userId,
        string userName,
        string? nickName,
        string connectionId,
        string deviceId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionId);
        ArgumentException.ThrowIfNullOrWhiteSpace(deviceId);

        var now = DateTime.UtcNow;
        var deviceKey = BuildDeviceKey(userId, deviceId);
        var record = ReadDevice(deviceKey) ?? new DeviceRecord
        {
            UserId = userId,
            DeviceId = deviceId,
            ConnectedAt = now
        };

        record.UserName = userName;
        record.NickName = nickName;
        record.LastSeenAt = now;
        if (!record.ConnectionIds.Contains(connectionId, StringComparer.Ordinal))
        {
            record.ConnectionIds.Add(connectionId);
        }

        WriteDevice(deviceKey, record);
        WriteConnIndex(connectionId, deviceKey);
        _database.SetAdd(_deviceIndexKey, deviceKey);
    }

    public void TouchConnection(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        var deviceKey = ReadConnIndex(connectionId);
        if (deviceKey == null)
        {
            return;
        }

        var record = ReadDevice(deviceKey);
        if (record == null)
        {
            _database.KeyDelete(BuildConnKey(connectionId));
            _database.SetRemove(_deviceIndexKey, deviceKey);
            return;
        }

        record.LastSeenAt = DateTime.UtcNow;
        WriteDevice(deviceKey, record);
        WriteConnIndex(connectionId, deviceKey);
    }

    public void RemoveConnection(string connectionId)
    {
        if (string.IsNullOrWhiteSpace(connectionId))
        {
            return;
        }

        var deviceKey = ReadConnIndex(connectionId);
        _database.KeyDelete(BuildConnKey(connectionId));
        if (deviceKey == null)
        {
            return;
        }

        var record = ReadDevice(deviceKey);
        if (record == null)
        {
            _database.SetRemove(_deviceIndexKey, deviceKey);
            return;
        }

        record.ConnectionIds.RemoveAll(x =>
            string.Equals(x, connectionId, StringComparison.Ordinal));

        if (record.ConnectionIds.Count == 0)
        {
            _database.KeyDelete(deviceKey);
            _database.SetRemove(_deviceIndexKey, deviceKey);
            return;
        }

        record.LastSeenAt = DateTime.UtcNow;
        WriteDevice(deviceKey, record);
    }

    public int RemoveUserConnections(Guid userId)
    {
        var removed = 0;
        foreach (var deviceKey in ListDeviceKeys())
        {
            var record = ReadDevice(deviceKey);
            if (record == null)
            {
                _database.SetRemove(_deviceIndexKey, deviceKey);
                continue;
            }

            if (record.UserId != userId)
            {
                continue;
            }

            foreach (var connectionId in record.ConnectionIds)
            {
                _database.KeyDelete(BuildConnKey(connectionId));
            }

            _database.KeyDelete(deviceKey);
            _database.SetRemove(_deviceIndexKey, deviceKey);
            removed++;
        }

        return removed;
    }

    public IReadOnlyList<OnlineUserDto> GetOnlineUsers()
    {
        var devices = LoadActiveDevices();
        return devices
            .GroupBy(x => x.UserId)
            .Select(group =>
            {
                var first = group.OrderBy(x => x.ConnectedAt).First();
                var last = group.OrderByDescending(x => x.LastSeenAt).First();

                return new OnlineUserDto
                {
                    UserId = first.UserId,
                    UserName = first.UserName,
                    NickName = first.NickName,
                    ConnectionCount = group.Count(),
                    ConnectedAt = first.ConnectedAt,
                    LastActiveAt = last.LastSeenAt
                };
            })
            .OrderByDescending(x => x.LastActiveAt)
            .ToList();
    }

    public int GetTotalConnectionCount() => LoadActiveDevices().Count;

    private List<DeviceRecord> LoadActiveDevices()
    {
        var result = new List<DeviceRecord>();
        foreach (var deviceKey in ListDeviceKeys())
        {
            var record = ReadDevice(deviceKey);
            if (record == null)
            {
                _database.SetRemove(_deviceIndexKey, deviceKey);
                continue;
            }

            result.Add(record);
        }

        return result;
    }

    private IEnumerable<string> ListDeviceKeys()
    {
        var members = _database.SetMembers(_deviceIndexKey);
        foreach (var member in members)
        {
            if (member.HasValue)
            {
                yield return member.ToString();
            }
        }
    }

    private void WriteDevice(string deviceKey, DeviceRecord record)
    {
        var payload = JsonSerializer.Serialize(record, JsonOptions);
        _database.StringSet(deviceKey, payload, TimeSpan.FromSeconds(PresenceTtlSeconds));
    }

    private DeviceRecord? ReadDevice(string deviceKey)
    {
        var value = _database.StringGet(deviceKey);
        if (!value.HasValue)
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<DeviceRecord>(value.ToString(), JsonOptions);
        }
        catch (JsonException)
        {
            _database.KeyDelete(deviceKey);
            return null;
        }
    }

    private void WriteConnIndex(string connectionId, string deviceKey)
    {
        _database.StringSet(
            BuildConnKey(connectionId),
            deviceKey,
            TimeSpan.FromSeconds(PresenceTtlSeconds));
    }

    private string? ReadConnIndex(string connectionId)
    {
        var value = _database.StringGet(BuildConnKey(connectionId));
        return value.HasValue ? value.ToString() : null;
    }

    private string BuildDeviceKey(Guid userId, string deviceId) =>
        $"{_keyPrefix}online:device:{userId:N}:{deviceId}";

    private string BuildConnKey(string connectionId) =>
        $"{_keyPrefix}online:conn:{connectionId}";

    private sealed class DeviceRecord
    {
        public Guid UserId { get; set; }
        public string DeviceId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string? NickName { get; set; }
        public List<string> ConnectionIds { get; set; } = [];
        public DateTime ConnectedAt { get; set; }
        public DateTime LastSeenAt { get; set; }
    }
}
