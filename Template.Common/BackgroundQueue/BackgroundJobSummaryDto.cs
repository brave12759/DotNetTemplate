namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作佇列統計資料。
/// </summary>
public class BackgroundJobSummaryDto
{
    /// <summary>
    /// 等待處理總數。
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 處理中總數。
    /// </summary>
    public int ProcessingCount { get; set; }

    /// <summary>
    /// 已完成總數。
    /// </summary>
    public int SucceededCount { get; set; }

    /// <summary>
    /// 失敗總數。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 已取消總數。
    /// </summary>
    public int CanceledCount { get; set; }

    /// <summary>
    /// 依工作類型分組的統計資料。
    /// </summary>
    public List<BackgroundJobWorkTypeSummaryDto> WorkTypes { get; set; } = [];
}

/// <summary>
/// 單一工作類型的佇列統計資料。
/// </summary>
public class BackgroundJobWorkTypeSummaryDto
{
    /// <summary>
    /// 工作類型。
    /// </summary>
    public BackgroundWorkType WorkType { get; set; }

    /// <summary>
    /// 工作類型名稱。
    /// </summary>
    public string WorkTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 等待處理數量。
    /// </summary>
    public int PendingCount { get; set; }

    /// <summary>
    /// 處理中數量。
    /// </summary>
    public int ProcessingCount { get; set; }

    /// <summary>
    /// 已完成數量。
    /// </summary>
    public int SucceededCount { get; set; }

    /// <summary>
    /// 失敗數量。
    /// </summary>
    public int FailedCount { get; set; }

    /// <summary>
    /// 已取消數量。
    /// </summary>
    public int CanceledCount { get; set; }
}
