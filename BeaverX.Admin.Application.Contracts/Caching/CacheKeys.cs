namespace BeaverX.Admin.Application.Contracts.Caching;

public static class CacheKeys
{
    public const string AccessVersion = "rbac:access:version";
    public const string MenuAll = "menu:all";
    public const string MenuTree = "menu:tree";
    public const string ConfigGroups = "config:groups";

    public static string ConfigByKey(string key) => $"config:key:{key.Trim()}";

    public static string UserMenus(long userId, long accessVersion) =>
        $"menu:user:{userId}:v{accessVersion}";

    public static string UserPermissions(long userId, long accessVersion) =>
        $"perm:user:{userId}:v{accessVersion}";

    public static string DictOptions(string typeCode) => $"dict:options:{typeCode.Trim()}";
}
