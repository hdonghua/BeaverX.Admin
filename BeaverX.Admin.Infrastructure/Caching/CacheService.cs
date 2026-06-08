using System.Text.Json;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Core.Dependency;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Infrastructure.Caching;

public class CacheService : ICacheService, ISingletonDependency
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    private readonly IDistributedCache _cache;
    private readonly CacheOptions _options;

    public CacheService(IDistributedCache cache, IOptions<CacheOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(BuildKey(key), cancellationToken);
        if (bytes == null || bytes.Length == 0)
        {
            return default;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)System.Text.Encoding.UTF8.GetString(bytes);
        }

        return JsonSerializer.Deserialize<T>(bytes, JsonOptions);
    }

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var bytes = Serialize(value);
        var entryOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration ?? TimeSpan.FromSeconds(_options.DefaultExpirationSeconds)
        };

        return _cache.SetAsync(BuildKey(key), bytes, entryOptions, cancellationToken);
    }

    public Task RemoveAsync(string key, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(BuildKey(key), cancellationToken);

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(BuildKey(key), cancellationToken);
        return bytes != null && bytes.Length > 0;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default)
    {
        if (await ExistsAsync(key, cancellationToken))
        {
            var cached = await GetAsync<T>(key, cancellationToken);
            if (cached is not null || !typeof(T).IsValueType)
            {
                return cached!;
            }
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, absoluteExpiration, cancellationToken);
        return value;
    }

    private string BuildKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key cannot be empty.", nameof(key));
        }

        var prefix = _options.KeyPrefix?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(prefix))
        {
            return key.Trim();
        }

        return prefix.EndsWith(':') ? $"{prefix}{key.Trim()}" : $"{prefix}:{key.Trim()}";
    }

    private static byte[] Serialize<T>(T value)
    {
        if (value is string text)
        {
            return System.Text.Encoding.UTF8.GetBytes(text);
        }

        return JsonSerializer.SerializeToUtf8Bytes(value, JsonOptions);
    }
}
