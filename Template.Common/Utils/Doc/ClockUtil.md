# ClockUtil 時間日期工具

[← 返回方案 README](../../../README.md) ｜ [← 返回上層 README](../../README.md)

`ClockUtil` 集中放置常用時間日期格式、解析、格式化與型別轉換工具，適用於 `DateTime`、`DateTimeOffset`、`DateOnly`、`TimeOnly`。

## 檔案位置

```text
Template.Common/Utils/ClockUtil.cs
```

命名空間：

```csharp
using Template.Common.Utils;
```

## 格式常數

| 常數 | 格式 |
|---|---|
| `ClockUtil.DateFormat` | `yyyy-MM-dd` |
| `ClockUtil.TimeFormat` | `HH:mm:ss` |
| `ClockUtil.DateTimeFormat` | `yyyy-MM-dd HH:mm:ss` |
| `ClockUtil.DateTimeMillisecondFormat` | `yyyy-MM-dd HH:mm:ss.fff` |
| `ClockUtil.UtcIsoFormat` | `yyyy-MM-ddTHH:mm:ss.fffZ` |

## 常用範例

### 取得目前時間

```csharp
var utcNow = ClockUtil.UtcNow();
var utcOffsetNow = ClockUtil.UtcNowOffset();
var taipeiNow = ClockUtil.LocalNow(TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));
var taipeiToday = ClockUtil.LocalToday(TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));
```

### 格式化

```csharp
var text = DateTime.UtcNow.Format();
var dateText = DateOnly.FromDateTime(DateTime.UtcNow).Format();
var timeText = TimeOnly.FromDateTime(DateTime.UtcNow).Format();
var isoText = DateTime.UtcNow.FormatUtcIso();
```

### 解析

```csharp
var dateTime = ClockUtil.ParseDateTime("2026-05-08 13:20:30");
var date = ClockUtil.ParseDateOnly("2026-05-08");
var time = ClockUtil.ParseTimeOnly("13:20:30");

if (ClockUtil.TryParseDateTime("2026-05-08 13:20:30", out var parsed))
{
    // parsed
}
```

### UTC 與本地時間轉換

```csharp
var taipei = TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

var localTime = new DateTime(2026, 5, 8, 13, 20, 30);
var utcTime = localTime.ToUtcDateTime(taipei);
var backToTaipei = utcTime.ToLocalDateTime(taipei);
```

`DateTimeKind.Unspecified` 搭配 `TimeZoneInfo` 時，會視為該時區的本地時間。

### DateOnly / TimeOnly / DateTime 互轉

```csharp
var date = DateOnly.FromDateTime(DateTime.UtcNow);
var time = TimeOnly.FromDateTime(DateTime.UtcNow);

var dateTime = date.ToDateTime(time);
var offset = date.ToDateTimeOffset(time, TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));

var dateOnly = DateTime.UtcNow.ToDateOnly();
var timeOnly = DateTime.UtcNow.ToTimeOnly();
```

### DateTimeOffset 轉換

```csharp
var offsetNow = DateTimeOffset.Now;
var utcOffset = offsetNow.ToUtcOffset();
var localOffset = utcOffset.ToLocalOffset(TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time"));
var dateOnly = localOffset.ToDateOnly();
var timeOnly = localOffset.ToTimeOnly();
```

### Unix Time

```csharp
var seconds = DateTimeOffset.UtcNow.ToUnixSeconds();
var milliseconds = DateTimeOffset.UtcNow.ToUnixMilliseconds();

var fromSeconds = ClockUtil.FromUnixSeconds(seconds);
var fromMilliseconds = ClockUtil.FromUnixMilliseconds(milliseconds);
```

### 日期區間

```csharp
var start = DateTime.UtcNow.StartOfDay();
var end = DateTime.UtcNow.EndOfDay();

var dateStart = DateOnly.FromDateTime(DateTime.UtcNow).StartOfDay();
var dateEnd = DateOnly.FromDateTime(DateTime.UtcNow).EndOfDay();
```

## 使用原則

- 資料庫儲存時間建議使用 UTC。
- 對外顯示前再依使用者或系統時區轉換。
- `DateOnly` 適合純日期，例如生日、假日。
- `TimeOnly` 適合純時間，例如每日排程時間。
- `DateTimeOffset` 適合保留原始 Offset 的外部交換資料。
