namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作類型。
/// </summary>
public enum BackgroundWorkType
{
    /// <summary>
    /// 附件上傳。
    /// </summary>
    AttachmentUpload = 1,

    /// <summary>
    /// 報表產製。
    /// </summary>
    Report = 2,

    /// <summary>
    /// 檔案下載。
    /// </summary>
    FileDownload = 3,

    /// <summary>
    /// M3U8 更新工作。
    /// </summary>
    M3u8Refresh = 4,

    /// <summary>
    /// SignalR 訊息推播。
    /// </summary>
    SignalRMessage = 5
}
