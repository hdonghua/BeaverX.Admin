using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Core.Dependency;

namespace BeaverX.Admin.Application.Caching;

public class AppCacheInvalidator : IScopedDependency
{
    private readonly ICacheService _cache;

    public AppCacheInvalidator(ICacheService cache)
    {
        _cache = cache;
    }

    public async Task<long> GetAccessVersionAsync(CancellationToken cancellationToken = default) =>
        await _cache.GetOrSetAsync(
            CacheKeys.AccessVersion,
            _ => Task.FromResult(DateTime.UtcNow.Ticks),
            CacheDurations.AccessVersion,
            cancellationToken);

    public Task BumpAccessVersionAsync(CancellationToken cancellationToken = default) =>
        _cache.SetAsync(
            CacheKeys.AccessVersion,
            DateTime.UtcNow.Ticks,
            CacheDurations.AccessVersion,
            cancellationToken);

    public async Task InvalidateMenusAsync(CancellationToken cancellationToken = default)
    {
        await _cache.RemoveAsync(CacheKeys.MenuAll, cancellationToken);
        await _cache.RemoveAsync(CacheKeys.MenuTree, cancellationToken);
        await BumpAccessVersionAsync(cancellationToken);
    }

    public async Task InvalidateConfigAsync(string? configKey = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(configKey))
        {
            await _cache.RemoveAsync(CacheKeys.ConfigByKey(configKey), cancellationToken);
        }

        await _cache.RemoveAsync(CacheKeys.ConfigGroups, cancellationToken);
    }

    public Task InvalidateDictOptionsAsync(string typeCode, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(CacheKeys.DictOptions(typeCode), cancellationToken);
}
