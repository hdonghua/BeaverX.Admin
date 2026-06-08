namespace BeaverX.Admin.Application.Contracts.Caching;

public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>驱动：Memory / Redis</summary>
    public string Driver { get; set; } = CacheDrivers.Memory;

    /// <summary>全局键前缀，如 beaverx:admin:</summary>
    public string KeyPrefix { get; set; } = "beaverx:admin:";

    /// <summary>Redis 连接串（Driver=Redis 时使用，可回退到 ConnectionStrings:Redis）</summary>
    public string? RedisConnectionString { get; set; }

    /// <summary>未指定过期时间时的默认 TTL（秒）</summary>
    public int DefaultExpirationSeconds { get; set; } = 3600;
}
