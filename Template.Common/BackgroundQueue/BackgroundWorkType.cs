namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作類型。
/// </summary>
public enum BackgroundWorkType
{
    /// <summary>
    /// 大型附件上傳。
    /// </summary>
    AttachmentUpload = 1,

    /// <summary>
    /// 報表產生。
    /// </summary>
    Report = 2,

    /// <summary>
    /// m3u8 播放清單更新。
    /// </summary>
    M3u8Refresh = 3
}
