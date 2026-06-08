using BeaverX.Admin.Application.Contracts.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BeaverX.Admin.Infrastructure.Caching;

internal static class CacheServiceCollectionExtensions
{
    public static void AddBeaverXCache(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));

        var cacheOptions = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>() ?? new CacheOptions();

        if (string.Equals(cacheOptions.Driver, CacheDrivers.Redis, StringComparison.OrdinalIgnoreCase))
        {
            var redisConnection = cacheOptions.RedisConnectionString
                ?? configuration.GetConnectionString("Redis")
                ?? throw new InvalidOperationException(
                    "Cache driver is Redis but no connection string was configured. " +
                    "Set Cache:RedisConnectionString or ConnectionStrings:Redis.");

            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
    }
}
