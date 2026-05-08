namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作狀態。
/// </summary>
public enum BackgroundJobStatus
{
    /// <summary>
    /// 等待執行。
    /// </summary>
    Pending = 0,

    /// <summary>
    /// 執行中。
    /// </summary>
    Processing = 1,

    /// <summary>
    /// 已完成。
    /// </summary>
    Succeeded = 2,

    /// <summary>
    /// 已失敗且不再重試。
    /// </summary>
    Failed = 3,

    /// <summary>
    /// 已取消。
    /// </summary>
    Canceled = 4
}
