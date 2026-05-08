using System.Text.Json;
using System.Text.Json.Serialization;

namespace Template.WebApi.Converters;

/// <summary>
/// 將 DateTime 序列化為目標時區的完整 ISO 8601 格式：yyyy-MM-ddTHH:mm:ss.fff+HH:mm
/// </summary>
public class DateTimeJsonConverter(TimeZoneInfo timeZone) : JsonConverter<DateTime>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffzzz";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTime.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var utc = value.Kind == DateTimeKind.Utc
            ? value
            : value.Kind == DateTimeKind.Local
                ? value.ToUniversalTime()
                : DateTime.SpecifyKind(value, DateTimeKind.Utc);

        var local = TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
        var offset = timeZone.GetUtcOffset(utc);
        writer.WriteStringValue(new DateTimeOffset(local, offset).ToString(Format));
    }
}

/// <summary>
/// 將 DateTimeOffset 序列化為目標時區的完整 ISO 8601 格式：yyyy-MM-ddTHH:mm:ss.fff+HH:mm
/// </summary>
public class DateTimeOffsetJsonConverter(TimeZoneInfo timeZone) : JsonConverter<DateTimeOffset>
{
    private const string Format = "yyyy-MM-ddTHH:mm:ss.fffzzz";

    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => DateTimeOffset.Parse(reader.GetString()!);

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        var converted = TimeZoneInfo.ConvertTime(value, timeZone);
        writer.WriteStringValue(converted.ToString(Format));
    }
}
