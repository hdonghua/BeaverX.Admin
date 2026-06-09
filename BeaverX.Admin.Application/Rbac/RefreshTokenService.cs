using System.Security.Cryptography;
using System.Text;
using BeaverX.Admin.Application.Contracts.Rbac;
using BeaverX.Admin.Domain.Rbac;
using BeaverX.Core.Dependency;
using BeaverX.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BeaverX.Admin.Application.Rbac;

public class RefreshTokenService : IScopedDependency
{
    private readonly IRepository<UserRefreshToken> _refreshTokenRepository;
    private readonly JwtOptions _options;

    public RefreshTokenService(
        IRepository<UserRefreshToken> refreshTokenRepository,
        IOptions<JwtOptions> options)
    {
        _refreshTokenRepository = refreshTokenRepository;
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
        long userId,
        CancellationToken cancellationToken = default)
    {
        var plainToken = GenerateToken();
        var entity = new UserRefreshToken
        {
            UserId = userId,
            TokenHash = HashToken(plainToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_options.RefreshTokenExpiresInDays),
        };

        await _refreshTokenRepository.InsertAsync(entity, cancellationToken: cancellationToken);
        return (plainToken, entity.ExpiresAt);
    }

    /// <summary>
    /// 一次性消费刷新令牌：标记 RevokedAt 并软删除，防止重复使用。
    /// </summary>
    public async Task<long?> TryConsumeAsync(
        string refreshToken,
        string? replacedByTokenHash = null,
        CancellationToken cancellationToken = default)
    {
        var hash = HashToken(refreshToken.Trim());
        var now = DateTime.UtcNow;

        var token = await _refreshTokenRepository.GetQueryable()
            .AsNoTracking()
            .Where(x =>
                x.TokenHash == hash &&
                x.RevokedAt == null &&
                !x.IsDeleted &&
                x.ExpiresAt > now)
            .Select(x => new { x.Id, x.UserId })
            .FirstOrDefaultAsync(cancellationToken);

        if (token == null)
        {
            return null;
        }

        var affected = await _refreshTokenRepository.GetQueryable()
            .Where(x => x.Id == token.Id && x.RevokedAt == null && !x.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletionTime, now)
                    .SetProperty(x => x.ReplacedByTokenHash, replacedByTokenHash),
                cancellationToken);

        return affected > 0 ? token.UserId : null;
    }

    public async Task RevokeAllForUserAsync(long userId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _refreshTokenRepository.GetQueryable()
            .Where(x => x.UserId == userId && x.RevokedAt == null && !x.IsDeleted)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.RevokedAt, now)
                    .SetProperty(x => x.IsDeleted, true)
                    .SetProperty(x => x.DeletionTime, now),
                cancellationToken);
    }
}
