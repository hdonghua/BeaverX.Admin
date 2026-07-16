using System.Collections.Concurrent;
using System.Reflection;
namespace BeaverX.Admin.SqlSugar.Interceptors;

/// <summary>
/// 将写入数据库的 DateTime 统一转为 UTC。
/// </summary>
public static class UtcDateTimeSaveChangesInterceptor
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> DateTimeProperties = new();

    public static void NormalizeDateTimes(object? entity)
    {
        if (entity == null)
        {
            return;
        }

        var properties = DateTimeProperties.GetOrAdd(
            entity.GetType(),
            static type => type
                .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .Where(property =>
                    property.CanRead &&
                    property.CanWrite &&
                    property.GetIndexParameters().Length == 0 &&
                    (property.PropertyType == typeof(DateTime) ||
                     property.PropertyType == typeof(DateTime?)))
                .ToArray());

        foreach (var property in properties)
        {
            if (property.GetValue(entity) is DateTime value)
            {
                property.SetValue(entity, ToUtc(value));
            }
        }
    }

    internal static DateTime ToUtc(DateTime value) =>
        value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };
}
