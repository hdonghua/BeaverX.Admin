using BeaverX.Admin.Application.Contracts.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace BeaverX.Admin.Infrastructure.Caching;

internal static class CacheServiceCollectionExtensions
{
    public static void AddBeaverXCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var redisConnection = RedisConnectionHelper.ResolveConnectionString(configuration);

        services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);

        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnection));

        services.TryAddSingleton(sp =>
            sp.GetRequiredService<IConnectionMultiplexer>().GetDatabase());
    }
}
