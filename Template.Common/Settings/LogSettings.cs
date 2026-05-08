namespace Template.Common.Settings;

public class LogSettings
{
    public const string SectionName = "LogSettings";

    /// <summary>
    /// 日誌輸出資料夾路徑，支援相對路徑與絕對路徑
    /// </summary>
    public string LogDirectory { get; set; } = "Logs";

    /// <summary>
    /// 單一日誌檔案大小上限（MB）
    /// </summary>
    public int FileSizeLimitMb { get; set; } = 50;

    /// <summary>
    /// 最多保留幾個日誌檔案（滾動時舊檔自動刪除）
    /// </summary>
    public int RetainedFileCountLimit { get; set; } = 100;

    /// <summary>
    /// 寫入檔案的最低日誌等級：Verbose / Debug / Information / Warning / Error / Fatal
    /// </summary>
    public string MinimumLevel { get; set; } = "Warning";
}
