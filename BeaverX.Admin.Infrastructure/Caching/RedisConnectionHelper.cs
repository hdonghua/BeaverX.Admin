using BeaverX.Admin.Application.Contracts.Caching;
using Microsoft.Extensions.Configuration;

namespace BeaverX.Admin.Infrastructure.Caching;

public static class RedisConnectionHelper
{
    public static string ResolveConnectionString(IConfiguration configuration)
    {
        var connectionString = configuration
            .GetSection(CacheOptions.SectionName)
            .Get<CacheOptions>()
            ?.RedisConnectionString;

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "Redis connection string is required. Set Cache:RedisConnectionString.");
        }

        return connectionString;
    }
}
