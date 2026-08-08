using BeaverX.Admin.Application.Contracts.Realtime;
using BeaverX.Admin.Infrastructure.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace BeaverX.Admin.Infrastructure.Realtime;

public static class RealtimeDistributedExtensions
{
    /// <summary>
    /// 注册 Redis <see cref="IDatabase"/> 与 <see cref="RedisOnlineUserTracker"/>。
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

    public static string ResolveRedisConnectionString(IConfiguration configuration) =>
        RedisConnectionHelper.ResolveConnectionString(configuration);
}
