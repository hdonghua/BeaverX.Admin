using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace BeaverX.Admin.Domain.Shared.Json;

public static class JsonIdSerializationExtensions
{
    private static readonly HashSet<string> ExcludedLongPropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Amount",
        "TotalAmount",
        "RefundedAmount",
        "Size",
        "Total",
    };

    public static void ConfigureSnowflakeIdJsonSerialization(JsonSerializerOptions options)
    {
        ConfigureUtcDateTimeJsonSerialization(options);
        options.TypeInfoResolver = new DefaultJsonTypeInfoResolver
        {
            Modifiers = { ApplyLongIdConverters }
        };
    }

    /// <summary>
    /// 注册全局 UTC DateTime JSON 转换器（DateTime / DateTime?）。
    /// </summary>
    public static void ConfigureUtcDateTimeJsonSerialization(JsonSerializerOptions options)
    {
        if (options.Converters.Any(c => c is UtcDateTimeJsonConverter))
        {
            return;
        }

        options.Converters.Add(new UtcDateTimeJsonConverter());
        options.Converters.Add(new NullableUtcDateTimeJsonConverter());
    }

    private static void ApplyLongIdConverters(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object)
        {
            return;
        }

        foreach (var property in typeInfo.Properties)
        {
            if (ShouldSerializeAsStringId(property.Name, property.PropertyType))
            {
                property.CustomConverter = CreateConverter(property.PropertyType);
            }
        }
    }

    private static bool ShouldSerializeAsStringId(string propertyName, Type? propertyType)
    {
        if (propertyType is null || ExcludedLongPropertyNames.Contains(propertyName))
        {
            return false;
        }

        if (propertyName.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
            propertyName.EndsWith("Id", StringComparison.OrdinalIgnoreCase))
        {
            return propertyType == typeof(long) || propertyType == typeof(long?);
        }

        if (propertyName.EndsWith("Ids", StringComparison.OrdinalIgnoreCase))
        {
            return propertyType == typeof(long[]) ||
                   propertyType == typeof(List<long>) ||
                   propertyType == typeof(IList<long>) ||
                   propertyType == typeof(IEnumerable<long>);
        }

        return false;
    }

    private static JsonConverter? CreateConverter(Type propertyType)
    {
        if (propertyType == typeof(long))
        {
            return new LongIdJsonConverter();
        }

        if (propertyType == typeof(long?))
        {
            return new NullableLongIdJsonConverter();
        }

        if (propertyType == typeof(long[]))
        {
            return new LongIdArrayJsonConverter();
        }

        if (propertyType == typeof(List<long>) ||
            propertyType == typeof(IList<long>) ||
            propertyType == typeof(IEnumerable<long>))
        {
            return new LongIdListJsonConverter();
        }

        return null;
    }
}
