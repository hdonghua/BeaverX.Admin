using System.Security.Cryptography;
using System.Text;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using Volo.Abp.DependencyInjection;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Rbac;

/// <summary>
/// ?????? Redis?TTL ???????????? used ?????????
/// </summary>
public class RefreshTokenService : IScopedDependency
{
    private readonly ICacheService _cache;
    private readonly JwtOptions _options;

    public RefreshTokenService(ICacheService cache, IOptions<JwtOptions> options)
    {
        _cache = cache;
        _options = options.Value;
    }

    public static string GenerateToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

    public static string HashToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes);
    }

    public async Task<(string RefreshToken, DateTime ExpiresAt)> CreateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var plainToken = GenerateToken();
        var hash = HashToken(plainToken);
        var expiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiresInDays);
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            throw new InvalidOperationException("Refresh token TTL must be positive.");
        }

        var entry = new RefreshTokenCacheEntry
        {
            UserId = userId,
            ExpiresAt = expiresAt
        };

        await _cache.SetAsync(CacheKeys.RefreshToken(hash), entry, ttl, cancellationToken);
        await AddToUserTokenListAsync(userId, hash, ttl, cancellationToken);
        return (plainToken, expiresAt);
    }

    /// <summary>
    /// ??????????????? key???? used ??????????
    /// ????????????????????????
    /// </summary>
    public async Task<Guid?> TryConsumeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken.Trim());
        var now = DateTime.UtcNow;
        var key = CacheKeys.RefreshToken(hash);

        var entry = await _cache.GetAsync<RefreshTokenCacheEntry>(key, cancellationToken);
        if (entry == null)
        {
            var used = await _cache.GetAsync<RefreshTokenUsedEntry>(
                CacheKeys.RefreshTokenUsed(hash),
                cancellationToken);
            if (used != null)
            {
                await RevokeAllForUserAsync(used.UserId, cancellationToken);
            }

            return null;
        }

        if (entry.ExpiresAt <= now)
        {
            await RemoveCachedTokenAsync(entry.UserId, hash, cancellationToken);
            return null;
        }

        var remaining = entry.ExpiresAt - now;
        await _cache.SetAsync(
            CacheKeys.RefreshTokenUsed(hash),
            new RefreshTokenUsedEntry { UserId = entry.UserId },
            remaining,
            cancellationToken);

        await RemoveCachedTokenAsync(entry.UserId, hash, cancellationToken);
        return entry.UserId;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RemoveAllCachedTokensForUserAsync(userId, cancellationToken);
    }

    private async Task AddToUserTokenListAsync(
        Guid userId,
        string hash,
        TimeSpan ttl,
        CancellationToken cancellationToken)
    {
        var key = CacheKeys.UserRefreshTokens(userId);
        var hashes = await _cache.GetAsync<List<string>>(key, cancellationToken) ?? [];
        if (!hashes.Contains(hash, StringComparer.Ordinal))
        {
            hashes.Add(hash);
        }

        // ???? TTL ????????? token ??????????
        var indexTtl = TimeSpan.FromDays(_options.RefreshTokenExpiresInDays);
        if (ttl > indexTtl)
        {
            indexTtl = ttl;
        }

        await _cache.SetAsync(key, hashes, indexTtl, cancellationToken);
    }

    private async Task RemoveCachedTokenAsync(
        Guid userId,
        string hash,
        CancellationToken cancellationToken)
    {
        await _cache.RemoveAsync(CacheKeys.RefreshToken(hash), cancellationToken);

        var key = CacheKeys.UserRefreshTokens(userId);
        var hashes = await _cache.GetAsync<List<string>>(key, cancellationToken);
        if (hashes == null || hashes.Count == 0)
        {
            return;
        }

        hashes.RemoveAll(x => x.Equals(hash, StringComparison.Ordinal));
        if (hashes.Count == 0)
        {
            await _cache.RemoveAsync(key, cancellationToken);
            return;
        }

        await _cache.SetAsync(
            key,
            hashes,
            TimeSpan.FromDays(_options.RefreshTokenExpiresInDays),
            cancellationToken);
    }

    private async Task RemoveAllCachedTokensForUserAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var key = CacheKeys.UserRefreshTokens(userId);
        var hashes = await _cache.GetAsync<List<string>>(key, cancellationToken);
        if (hashes != null)
        {
            foreach (var hash in hashes)
            {
                await _cache.RemoveAsync(CacheKeys.RefreshToken(hash), cancellationToken);
                await _cache.RemoveAsync(CacheKeys.RefreshTokenUsed(hash), cancellationToken);
            }
        }

        await _cache.RemoveAsync(key, cancellationToken);
    }

    private sealed class RefreshTokenCacheEntry
    {
        public Guid UserId { get; init; }
        public DateTime ExpiresAt { get; init; }
    }

    private sealed class RefreshTokenUsedEntry
    {
        public Guid UserId { get; init; }
    }
}
