using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Utils;

namespace Template.Test.Tests;

[TestClass]
public class ClockUtilTests
{
    private static readonly TimeZoneInfo PlusEightTimeZone = TimeZoneInfo.CreateCustomTimeZone(
        "UTC+08",
        TimeSpan.FromHours(8),
        "UTC+08",
        "UTC+08");

    [TestMethod]
    public void Format_Should_Use_Default_Formats()
    {
        var dateTime = new DateTime(2026, 5, 8, 13, 20, 30, 456, DateTimeKind.Utc);
        var date = new DateOnly(2026, 5, 8);
        var time = new TimeOnly(13, 20, 30);

        Assert.AreEqual("2026-05-08 13:20:30", dateTime.Format());
        Assert.AreEqual("2026-05-08 13:20:30.456", dateTime.Format(ClockUtil.DateTimeMillisecondFormat));
        Assert.AreEqual("2026-05-08", date.Format());
        Assert.AreEqual("13:20:30", time.Format());
    }

    [TestMethod]
    public void Parse_Should_Parse_DateTime_DateOnly_TimeOnly()
    {
        var dateTime = ClockUtil.ParseDateTime("2026-05-08 13:20:30", kind: DateTimeKind.Utc);
        var date = ClockUtil.ParseDateOnly("2026-05-08");
        var time = ClockUtil.ParseTimeOnly("13:20:30");

        Assert.AreEqual(new DateTime(2026, 5, 8, 13, 20, 30, DateTimeKind.Utc), dateTime);
        Assert.AreEqual(new DateOnly(2026, 5, 8), date);
        Assert.AreEqual(new TimeOnly(13, 20, 30), time);
    }

    [TestMethod]
    public void TryParseDateTime_InvalidValue_Should_ReturnFalse()
    {
        var ok = ClockUtil.TryParseDateTime("not-a-date", out var result);

        Assert.IsFalse(ok);
        Assert.AreEqual(default, result);
    }

    [TestMethod]
    public void ParseDateTime_InvalidValue_Should_ThrowFormatException()
    {
        Assert.ThrowsException<FormatException>(() => ClockUtil.ParseDateTime("not-a-date"));
    }

    [TestMethod]
    public void ToUtcDateTime_Utc_Should_ReturnSameValue()
    {
        var utc = new DateTime(2026, 5, 8, 5, 0, 0, DateTimeKind.Utc);

        var result = utc.ToUtcDateTime(PlusEightTimeZone);

        Assert.AreEqual(utc, result);
        Assert.AreEqual(DateTimeKind.Utc, result.Kind);
    }

    [TestMethod]
    public void ToUtcDateTime_UnspecifiedWithTimeZone_Should_ConvertToUtc()
    {
        var local = new DateTime(2026, 5, 8, 13, 0, 0, DateTimeKind.Unspecified);

        var utc = local.ToUtcDateTime(PlusEightTimeZone);

        Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
        Assert.AreEqual(new DateTime(2026, 5, 8, 5, 0, 0, DateTimeKind.Utc), utc);
    }

    [TestMethod]
    public void ToUtcDateTime_LocalWithoutTimeZone_Should_ConvertUsingLocalKind()
    {
        var local = new DateTime(2026, 5, 8, 13, 0, 0, DateTimeKind.Local);

        var utc = local.ToUtcDateTime();

        Assert.AreEqual(DateTimeKind.Utc, utc.Kind);
        Assert.AreEqual(local.ToUniversalTime(), utc);
    }

    [TestMethod]
    public void ToLocalDateTime_Utc_Should_ConvertToTargetTimeZone()
    {
        var utc = new DateTime(2026, 5, 8, 5, 0, 0, DateTimeKind.Utc);

        var local = utc.ToLocalDateTime(PlusEightTimeZone);

        Assert.AreEqual(new DateTime(2026, 5, 8, 13, 0, 0), local);
    }

    [TestMethod]
    public void DateOnlyAndTimeOnly_Should_Convert_ToDateTimeAndOffset()
    {
        var date = new DateOnly(2026, 5, 8);
        var time = new TimeOnly(13, 20, 30);

        var dateTime = date.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = date.ToDateTimeOffset(time, PlusEightTimeZone);

        Assert.AreEqual(new DateTime(2026, 5, 8, 13, 20, 30), dateTime);
        Assert.AreEqual(TimeSpan.FromHours(8), offset.Offset);
        Assert.AreEqual(date, offset.ToDateOnly());
        Assert.AreEqual(time, offset.ToTimeOnly());
    }

    [TestMethod]
    public void ToDateTimeOffset_Utc_Should_UseZeroOffset()
    {
        var utc = new DateTime(2026, 5, 8, 5, 0, 0, DateTimeKind.Utc);

        var offset = utc.ToDateTimeOffset(PlusEightTimeZone);

        Assert.AreEqual(TimeSpan.Zero, offset.Offset);
        Assert.AreEqual(utc, offset.UtcDateTime);
    }

    [TestMethod]
    public void DateTimeOffset_ToDateOnly_WithTimeZone_Should_UseTargetDate()
    {
        var utcLateNight = new DateTimeOffset(2026, 5, 8, 18, 30, 0, TimeSpan.Zero);

        var date = utcLateNight.ToDateOnly(PlusEightTimeZone);

        Assert.AreEqual(new DateOnly(2026, 5, 9), date);
    }

    [TestMethod]
    public void DateTime_Should_Convert_ToDateOnlyAndTimeOnly()
    {
        var value = new DateTime(2026, 5, 8, 13, 20, 30);

        Assert.AreEqual(new DateOnly(2026, 5, 8), value.ToDateOnly());
        Assert.AreEqual(new TimeOnly(13, 20, 30), value.ToTimeOnly());
    }

    [TestMethod]
    public void StartOfDayAndEndOfDay_Should_ReturnBoundary()
    {
        var value = new DateTime(2026, 5, 8, 13, 20, 30, DateTimeKind.Utc);

        Assert.AreEqual(new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc), value.StartOfDay());
        Assert.AreEqual(new DateTime(2026, 5, 8, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), value.EndOfDay());
    }

    [TestMethod]
    public void UnixTime_Should_RoundTrip()
    {
        var value = new DateTimeOffset(2026, 5, 8, 13, 20, 30, TimeSpan.Zero);

        var seconds = value.ToUnixSeconds();
        var milliseconds = value.ToUnixMilliseconds();

        Assert.AreEqual(value, ClockUtil.FromUnixSeconds(seconds));
        Assert.AreEqual(value, ClockUtil.FromUnixMilliseconds(milliseconds));
    }

    [TestMethod]
    public void FormatUtcIso_Should_UseUtcIsoFormat()
    {
        var value = new DateTimeOffset(2026, 5, 8, 13, 20, 30, 456, TimeSpan.FromHours(8));

        var text = value.FormatUtcIso();

        Assert.AreEqual("2026-05-08T05:20:30.456Z", text);
    }

    [TestMethod]
    public void NowMethods_Should_ReturnReasonableValues()
    {
        var utcNow = ClockUtil.UtcNow();
        var utcOffsetNow = ClockUtil.UtcNowOffset();
        var localNow = ClockUtil.LocalNow(PlusEightTimeZone);
        var localToday = ClockUtil.LocalToday(PlusEightTimeZone);

        Assert.AreEqual(DateTimeKind.Utc, utcNow.Kind);
        Assert.IsTrue(utcOffsetNow.Offset == TimeSpan.Zero);
        Assert.AreEqual(DateOnly.FromDateTime(localNow), localToday);
    }

    [TestMethod]
    public void OffsetAndKindHelpers_Should_Work()
    {
        var dt = new DateTime(2026, 5, 8, 13, 20, 30, DateTimeKind.Unspecified);
        var withKind = dt.WithKind(DateTimeKind.Local);
        var dto = new DateTimeOffset(2026, 5, 8, 13, 20, 30, TimeSpan.FromHours(8));

        Assert.AreEqual(DateTimeKind.Local, withKind.Kind);
        Assert.AreEqual(TimeSpan.Zero, dto.ToUtcOffset().Offset);
        Assert.AreEqual(TimeSpan.FromHours(8), dto.ToLocalOffset(PlusEightTimeZone).Offset);
    }

    [TestMethod]
    public void DateOnlyBoundaryHelpers_Should_Work()
    {
        var date = new DateOnly(2026, 5, 8);

        var start = date.StartOfDay(DateTimeKind.Utc);
        var end = date.EndOfDay(DateTimeKind.Utc);

        Assert.AreEqual(new DateTime(2026, 5, 8, 0, 0, 0, DateTimeKind.Utc), start);
        Assert.AreEqual(new DateTime(2026, 5, 8, 23, 59, 59, 999, DateTimeKind.Utc).AddTicks(9999), end);
    }
}
