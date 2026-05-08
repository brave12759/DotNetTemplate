namespace Template.Common.Settings;

public class TimeZoneSettings
{
    public const string SectionName = "TimeZoneSettings";

    /// <summary>
    /// IANA 時區 ID，例如 "Asia/Taipei"（等同 Windows 的 "Taipei Standard Time"）
    /// </summary>
    public string TimeZoneId { get; set; } = "Asia/Taipei";
}
