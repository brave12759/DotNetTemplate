using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Template.DataAccess.Converters;

/// <summary>
/// EF Core DateTime UTC 值轉換器。
/// 寫入 DB：將 DateTime 統一轉為 UTC（Unspecified 視為 UTC）。
/// 讀出 DB：標記 DateTimeKind.Utc，確保後續時區轉換正確。
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    v => v.Kind == DateTimeKind.Utc
             ? v
             : v.Kind == DateTimeKind.Local
                 ? v.ToUniversalTime()
                 : DateTime.SpecifyKind(v, DateTimeKind.Utc),
    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
