namespace BeaverX.Admin.Application.Caching;

internal static class CacheDurations
{
    public static readonly TimeSpan Config = TimeSpan.FromHours(1);
    public static readonly TimeSpan Menu = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan UserAccess = TimeSpan.FromMinutes(30);
    public static readonly TimeSpan Dict = TimeSpan.FromHours(1);
    public static readonly TimeSpan AccessVersion = TimeSpan.FromDays(365);
}
