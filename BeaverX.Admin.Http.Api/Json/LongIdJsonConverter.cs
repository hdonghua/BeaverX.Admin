using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeaverX.Admin.Http.Api.Json;

/// <summary>
/// 将 long / long? 序列化为 JSON string，避免前端 Number 精度丢失。
/// </summary>
public sealed class LongIdJsonConverter : JsonConverter<long>
{
    public override long Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString();
            if (string.IsNullOrWhiteSpace(text))
            {
                return 0;
            }

            if (long.TryParse(text, out var parsed))
            {
                return parsed;
            }

            throw new JsonException($"Cannot parse long id from string '{text}'.");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return reader.GetInt64();
        }

        throw new JsonException($"Unexpected token {reader.TokenType} when parsing long id.");
    }

    public override void Write(Utf8JsonWriter writer, long value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public sealed class NullableLongIdJsonConverter : JsonConverter<long?>
{
    public override long? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        return new LongIdJsonConverter().Read(ref reader, typeof(long), options);
    }

    public override void Write(Utf8JsonWriter writer, long? value, JsonSerializerOptions options)
    {
        if (value is null)
        {
            writer.WriteNullValue();
            return;
        }

        writer.WriteStringValue(value.Value.ToString());
    }
}

public sealed class LongIdArrayJsonConverter : JsonConverter<long[]>
{
    public override long[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException("Expected start of array when parsing long id array.");
        }

        var values = new List<long>();
        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return values.ToArray();
            }

            values.Add(new LongIdJsonConverter().Read(ref reader, typeof(long), options));
        }

        throw new JsonException("Unexpected end when parsing long id array.");
    }

    public override void Write(Utf8JsonWriter writer, long[] value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        foreach (var item in value)
        {
            writer.WriteStringValue(item.ToString());
        }

        writer.WriteEndArray();
    }
}

public sealed class LongIdListJsonConverter : JsonConverter<List<long>>
{
    public override List<long> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new LongIdArrayJsonConverter().Read(ref reader, typeof(long[]), options).ToList();
    }

    public override void Write(Utf8JsonWriter writer, List<long> value, JsonSerializerOptions options)
    {
        new LongIdArrayJsonConverter().Write(writer, value.ToArray(), options);
    }
}
