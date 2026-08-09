namespace BeaverX.Admin.Application.Contracts.Caching;

public static class CacheKeys
{
    public const string AccessVersion = "rbac:access:version";
    public const string MenuAll = "menu:all";
    public const string MenuTree = "menu:tree";
    public const string ConfigGroups = "config:groups";

    public static string ConfigByKey(string key) => $"config:key:{key.Trim()}";

    public static string UserMenus(Guid userId, long accessVersion) =>
        $"menu:user:{userId}:v{accessVersion}";

    public static string UserPermissions(Guid userId, long accessVersion) =>
        $"perm:user:{userId}:v{accessVersion}";

    public static string DictOptions(string typeCode) => $"dict:options:{typeCode.Trim()}";

    public static string RefreshToken(string tokenHash) => $"auth:refresh:token:{tokenHash}";

    /// <summary>已消费的刷新令牌标记（用于复用检测，TTL 与原令牌剩余寿命一致）。</summary>
    public static string RefreshTokenUsed(string tokenHash) => $"auth:refresh:used:{tokenHash}";

    public static string UserRefreshTokens(Guid userId) => $"auth:refresh:user:{userId}";
}
