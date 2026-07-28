using System.Security.Cryptography;
using System.Text;
using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Rbac;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Rbac;

public class RefreshTokenService : IScopedDependency
{
    private readonly IRepository<UserRefreshToken, Guid> _refreshTokenRepository;
    private readonly ICacheService _cache;
    private readonly JwtOptions _options;

    public RefreshTokenService(
        IRepository<UserRefreshToken, Guid> refreshTokenRepository,
        ICacheService cache,
        IOptions<JwtOptions> options)
    {
        _refreshTokenRepository = refreshTokenRepository;
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
        var entity = new UserRefreshToken
        {
            UserId = userId,
            TokenHash = hash,
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiresInDays),
        };

        await _refreshTokenRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        await CacheTokenAsync(userId, hash, entity.Id, entity.ExpiresAt, cancellationToken);
        return (plainToken, entity.ExpiresAt);
    }

    /// <summary>
    /// 一次性消费刷新令牌：标记 RevokedAt 并软删除，防止重复使用�?
    /// 优先读缓存校验，命中时跳过数据库查询�?
    /// </summary>
    public async Task<Guid?> TryConsumeAsync(
        string refreshToken,
        string? replacedByTokenHash = null,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken.Trim());
        var now = DateTime.UtcNow;

        var cached = await _cache.GetAsync<RefreshTokenCacheEntry>(
            CacheKeys.RefreshToken(hash),
            cancellationToken);

        if (cached != null)
        {
            if (cached.ExpiresAt <= now)
            {
                await RemoveCachedTokenAsync(cached.UserId, hash, cancellationToken);
                return null;
            }

            var affected = await RevokeByHashAsync(hash, now, replacedByTokenHash, cancellationToken);
            await RemoveCachedTokenAsync(cached.UserId, hash, cancellationToken);
            return affected > 0 ? cached.UserId : null;
        }

        var token = await (await _refreshTokenRepository.GetQueryableAsync())
            .AsNoTracking()
            .Where(x =>
                x.TokenHash == hash &&
                x.RevokedAt == null &&
                !x.IsDeleted &&
                x.ExpiresAt > now)
            .Select(x => new { x.Id, x.UserId, x.ExpiresAt })
            .FirstOrDefaultAsync(cancellationToken);

        if (token == null)
        {
            return null;
        }

        var revoked = await RevokeByIdAsync(token.Id, now, replacedByTokenHash, cancellationToken);
        if (revoked)
        {
            await RemoveCachedTokenAsync(token.UserId, hash, cancellationToken);
            return token.UserId;
        }

        return null;
    }

    public async Task RevokeAllForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        await RemoveAllCachedTokensForUserAsync(userId, cancellationToken);

        var now = DateTime.UtcNow;
        (await _refreshTokenRepository.GetQueryableAsync())
            .Where(x => x.UserId == userId && x.RevokedAt == null && !x.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletionTime, now),
                cancellationToken);
    }

    private async Task CacheTokenAsync(
        Guid userId,
        string hash,
        Guid tokenId,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var ttl = expiresAt - DateTime.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        var entry = new RefreshTokenCacheEntry
        {
            UserId = userId,
            TokenId = tokenId,
            ExpiresAt = expiresAt,
        };

        await _cache.SetAsync(CacheKeys.RefreshToken(hash), entry, ttl, cancellationToken);
        await AddToUserTokenListAsync(userId, hash, ttl, cancellationToken);
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

        await _cache.SetAsync(key, hashes, ttl, cancellationToken);
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

        var ttl = TimeSpan.FromDays(_options.RefreshTokenExpiresInDays);
        await _cache.SetAsync(key, hashes, ttl, cancellationToken);
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
            }
        }

        await _cache.RemoveAsync(key, cancellationToken);
    }

    private async Task<int> RevokeByHashAsync(
        string hash,
        DateTime now,
        string? replacedByTokenHash,
        CancellationToken cancellationToken)
    {
        return await (await _refreshTokenRepository.GetQueryableAsync())
            .Where(x => x.TokenHash == hash && x.RevokedAt == null && !x.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletionTime, now)
                    .SetProperty(x => x.ReplacedByTokenHash, replacedByTokenHash),
                cancellationToken);
    }

    private async Task<bool> RevokeByIdAsync(
        Guid tokenId,
        DateTime now,
        string? replacedByTokenHash,
        CancellationToken cancellationToken)
    {
        var affected = await (await _refreshTokenRepository.GetQueryableAsync())
            .Where(x => x.Id == tokenId && x.RevokedAt == null && !x.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletionTime, now)
                    .SetProperty(x => x.ReplacedByTokenHash, replacedByTokenHash),
                cancellationToken);

        return affected > 0;
    }

    private sealed class RefreshTokenCacheEntry
    {
        public Guid UserId { get; init; }
        public Guid TokenId { get; init; }
        public DateTime ExpiresAt { get; init; }
    }
}
