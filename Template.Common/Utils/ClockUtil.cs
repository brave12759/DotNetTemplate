using System.Globalization;

namespace Template.Common.Utils;

/// <summary>
/// 時間與日期常用工具。
/// </summary>
public static class ClockUtil
{
    /// <summary>
    /// 日期格式：yyyy-MM-dd。
    /// </summary>
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>
    /// 時間格式：HH:mm:ss。
    /// </summary>
    public const string TimeFormat = "HH:mm:ss";

    /// <summary>
    /// 日期時間格式：yyyy-MM-dd HH:mm:ss。
    /// </summary>
    public const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss";

    /// <summary>
    /// 含毫秒日期時間格式：yyyy-MM-dd HH:mm:ss.fff。
    /// </summary>
    public const string DateTimeMillisecondFormat = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// UTC ISO 8601 格式：yyyy-MM-ddTHH:mm:ss.fffZ。
    /// </summary>
    public const string UtcIsoFormat = "yyyy-MM-ddTHH:mm:ss.fffZ";

    /// <summary>
    /// 取得目前 UTC 時間。
    /// </summary>
    public static DateTime UtcNow() => DateTime.UtcNow;

    /// <summary>
    /// 取得目前 UTC 時間，保留 Offset。
    /// </summary>
    public static DateTimeOffset UtcNowOffset() => DateTimeOffset.UtcNow;

    /// <summary>
    /// 取得指定時區目前時間。
    /// </summary>
    public static DateTime LocalNow(TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

    /// <summary>
    /// 取得指定時區目前日期。
    /// </summary>
    public static DateOnly LocalToday(TimeZoneInfo timeZone) =>
        DateOnly.FromDateTime(LocalNow(timeZone));

    /// <summary>
    /// 使用固定格式解析 DateTime。
    /// </summary>
    public static DateTime ParseDateTime(
        string value,
        string format = DateTimeFormat,
        DateTimeKind kind = DateTimeKind.Unspecified,
        IFormatProvider? provider = null)
    {
        var parsed = DateTime.ParseExact(value, format, provider ?? CultureInfo.InvariantCulture);
        return DateTime.SpecifyKind(parsed, kind);
    }

    /// <summary>
    /// 嘗試使用固定格式解析 DateTime。
    /// </summary>
    public static bool TryParseDateTime(
        string value,
        out DateTime result,
        string format = DateTimeFormat,
        DateTimeKind kind = DateTimeKind.Unspecified,
        IFormatProvider? provider = null)
    {
        var ok = DateTime.TryParseExact(
            value,
            format,
            provider ?? CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var parsed);

        result = ok ? DateTime.SpecifyKind(parsed, kind) : default;
        return ok;
    }

    /// <summary>
    /// 使用固定格式解析 DateOnly。
    /// </summary>
    public static DateOnly ParseDateOnly(
        string value,
        string format = DateFormat,
        IFormatProvider? provider = null) =>
        DateOnly.ParseExact(value, format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// 使用固定格式解析 TimeOnly。
    /// </summary>
    public static TimeOnly ParseTimeOnly(
        string value,
        string format = TimeFormat,
        IFormatProvider? provider = null) =>
        TimeOnly.ParseExact(value, format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// 指定 DateTimeKind。
    /// </summary>
    public static DateTime WithKind(this DateTime value, DateTimeKind kind) =>
        DateTime.SpecifyKind(value, kind);

    /// <summary>
    /// 將 DateTime 轉為 UTC。
    /// 若值為 Unspecified 且有指定時區，會視為該時區時間。
    /// </summary>
    public static DateTime ToUtcDateTime(this DateTime value, TimeZoneInfo? timeZone = null)
    {
        if (value.Kind == DateTimeKind.Utc)
            return value;

        if (value.Kind == DateTimeKind.Local && timeZone is null)
            return value.ToUniversalTime();

        if (timeZone is null)
            return DateTime.SpecifyKind(value, DateTimeKind.Local).ToUniversalTime();

        var unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        return TimeZoneInfo.ConvertTimeToUtc(unspecified, timeZone);
    }

    /// <summary>
    /// 將 UTC DateTime 轉為指定時區時間。
    /// </summary>
    public static DateTime ToLocalDateTime(this DateTime utcValue, TimeZoneInfo timeZone)
    {
        var utc = utcValue.Kind == DateTimeKind.Utc
            ? utcValue
            : DateTime.SpecifyKind(utcValue, DateTimeKind.Utc);

        return TimeZoneInfo.ConvertTimeFromUtc(utc, timeZone);
    }

    /// <summary>
    /// 將 DateTimeOffset 轉為 UTC DateTimeOffset。
    /// </summary>
    public static DateTimeOffset ToUtcOffset(this DateTimeOffset value) =>
        value.ToUniversalTime();

    /// <summary>
    /// 將 DateTimeOffset 轉為指定時區 DateTimeOffset。
    /// </summary>
    public static DateTimeOffset ToLocalOffset(this DateTimeOffset value, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(value, timeZone);

    /// <summary>
    /// 將 DateTime 轉為 DateTimeOffset。
    /// 若 DateTime 為 Unspecified，會視為指定時區時間。
    /// </summary>
    public static DateTimeOffset ToDateTimeOffset(this DateTime value, TimeZoneInfo? timeZone = null)
    {
        if (value.Kind == DateTimeKind.Utc)
            return new DateTimeOffset(value, TimeSpan.Zero);

        if (value.Kind == DateTimeKind.Local && timeZone is null)
            return new DateTimeOffset(value);

        if (timeZone is null)
            return new DateTimeOffset(value);

        var unspecified = DateTime.SpecifyKind(value, DateTimeKind.Unspecified);
        return new DateTimeOffset(unspecified, timeZone.GetUtcOffset(unspecified));
    }

    /// <summary>
    /// 取出 DateTime 的日期部分。
    /// </summary>
    public static DateOnly ToDateOnly(this DateTime value) =>
        DateOnly.FromDateTime(value);

    /// <summary>
    /// 取出 DateTimeOffset 在指定時區的日期部分。
    /// 未指定時區時使用該值本身的日期部分。
    /// </summary>
    public static DateOnly ToDateOnly(this DateTimeOffset value, TimeZoneInfo? timeZone = null)
    {
        var local = timeZone is null ? value : TimeZoneInfo.ConvertTime(value, timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    /// <summary>
    /// 取出 DateTime 的時間部分。
    /// </summary>
    public static TimeOnly ToTimeOnly(this DateTime value) =>
        TimeOnly.FromDateTime(value);

    /// <summary>
    /// 取出 DateTimeOffset 在指定時區的時間部分。
    /// 未指定時區時使用該值本身的時間部分。
    /// </summary>
    public static TimeOnly ToTimeOnly(this DateTimeOffset value, TimeZoneInfo? timeZone = null)
    {
        var local = timeZone is null ? value : TimeZoneInfo.ConvertTime(value, timeZone);
        return TimeOnly.FromDateTime(local.DateTime);
    }

    /// <summary>
    /// 合併 DateOnly 與 TimeOnly 為 DateTime。
    /// </summary>
    public static DateTime ToDateTime(
        this DateOnly date,
        TimeOnly? time = null,
        DateTimeKind kind = DateTimeKind.Unspecified)
    {
        var dateTime = date.ToDateTime(time ?? TimeOnly.MinValue);
        return DateTime.SpecifyKind(dateTime, kind);
    }

    /// <summary>
    /// 合併 DateOnly 與 TimeOnly 為指定時區的 DateTimeOffset。
    /// </summary>
    public static DateTimeOffset ToDateTimeOffset(
        this DateOnly date,
        TimeOnly time,
        TimeZoneInfo timeZone)
    {
        var dateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        return new DateTimeOffset(dateTime, timeZone.GetUtcOffset(dateTime));
    }

    /// <summary>
    /// 取得當日開始時間 00:00:00。
    /// </summary>
    public static DateTime StartOfDay(this DateTime value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, value.Kind);

    /// <summary>
    /// 取得當日結束時間 23:59:59.9999999。
    /// </summary>
    public static DateTime EndOfDay(this DateTime value) =>
        value.StartOfDay().AddDays(1).AddTicks(-1);

    /// <summary>
    /// 取得 DateOnly 當日開始時間。
    /// </summary>
    public static DateTime StartOfDay(this DateOnly value, DateTimeKind kind = DateTimeKind.Unspecified) =>
        value.ToDateTime(TimeOnly.MinValue, kind);

    /// <summary>
    /// 取得 DateOnly 當日結束時間。
    /// </summary>
    public static DateTime EndOfDay(this DateOnly value, DateTimeKind kind = DateTimeKind.Unspecified) =>
        value.StartOfDay(kind).AddDays(1).AddTicks(-1);

    /// <summary>
    /// 轉換為 Unix 秒數。
    /// </summary>
    public static long ToUnixSeconds(this DateTimeOffset value) =>
        value.ToUnixTimeSeconds();

    /// <summary>
    /// 轉換為 Unix 毫秒數。
    /// </summary>
    public static long ToUnixMilliseconds(this DateTimeOffset value) =>
        value.ToUnixTimeMilliseconds();

    /// <summary>
    /// 從 Unix 秒數建立 UTC DateTimeOffset。
    /// </summary>
    public static DateTimeOffset FromUnixSeconds(long seconds) =>
        DateTimeOffset.FromUnixTimeSeconds(seconds);

    /// <summary>
    /// 從 Unix 毫秒數建立 UTC DateTimeOffset。
    /// </summary>
    public static DateTimeOffset FromUnixMilliseconds(long milliseconds) =>
        DateTimeOffset.FromUnixTimeMilliseconds(milliseconds);

    /// <summary>
    /// 使用指定格式輸出 DateTime。
    /// </summary>
    public static string Format(this DateTime value, string format = DateTimeFormat, IFormatProvider? provider = null) =>
        value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// 使用指定格式輸出 DateTimeOffset。
    /// </summary>
    public static string Format(this DateTimeOffset value, string format = DateTimeFormat, IFormatProvider? provider = null) =>
        value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// 使用指定格式輸出 DateOnly。
    /// </summary>
    public static string Format(this DateOnly value, string format = DateFormat, IFormatProvider? provider = null) =>
        value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// 使用指定格式輸出 TimeOnly。
    /// </summary>
    public static string Format(this TimeOnly value, string format = TimeFormat, IFormatProvider? provider = null) =>
        value.ToString(format, provider ?? CultureInfo.InvariantCulture);

    /// <summary>
    /// 使用 UTC ISO 8601 格式輸出 DateTime。
    /// </summary>
    public static string FormatUtcIso(this DateTime value) =>
        value.ToUtcDateTime().ToString(UtcIsoFormat, CultureInfo.InvariantCulture);

    /// <summary>
    /// 使用 UTC ISO 8601 格式輸出 DateTimeOffset。
    /// </summary>
    public static string FormatUtcIso(this DateTimeOffset value) =>
        value.ToUniversalTime().ToString(UtcIsoFormat, CultureInfo.InvariantCulture);
}
