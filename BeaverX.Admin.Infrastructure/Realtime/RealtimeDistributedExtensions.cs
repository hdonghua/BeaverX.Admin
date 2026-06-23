using BeaverX.Admin.Application.Contracts.Caching;
using BeaverX.Admin.Application.Contracts.Realtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace BeaverX.Admin.Infrastructure.Realtime;

public static class RealtimeDistributedExtensions
{
    /// <summary>
    /// 注册 Redis <see cref="IDatabase"/> 并将 <see cref="IOnlineUserTracker"/> 替换为 <see cref="RedisOnlineUserTracker"/>。
    /// 需在 Infrastructure 模块注册之后调用（会覆盖默认内存实现）。
    /// </summary>
    public static IServiceCollection AddRedisOnlineUserTracker(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var redisConnection = ResolveRedisConnectionString(configuration);

        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());

        services.Replace(ServiceDescriptor.Singleton<IOnlineUserTracker, RedisOnlineUserTracker>());

        return services;
    }

    internal static string ResolveRedisConnectionString(IConfiguration configuration)
    {
        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>();

        var connectionString = cacheOptions?.RedisConnectionString
            ?? configuration.GetConnectionString("Redis");

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Redis connection string is required for distributed online user tracking. " +
                "Set Cache:RedisConnectionString or ConnectionStrings:Redis.");
        }

        return connectionString;
    }
}
