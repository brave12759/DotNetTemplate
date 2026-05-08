namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作明細資料。
/// </summary>
public class BackgroundJobDto
{
    /// <summary>
    /// 工作流水號。
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// 工作類型。
    /// </summary>
    public BackgroundWorkType WorkType { get; set; }

    /// <summary>
    /// 工作類型名稱。
    /// </summary>
    public string WorkTypeName { get; set; } = string.Empty;

    /// <summary>
    /// 前端或業務功能用來識別同一批工作的鍵值。
    /// </summary>
    public string WorkKey { get; set; } = string.Empty;

    /// <summary>
    /// 工作參數 JSON。
    /// </summary>
    public string PayloadJson { get; set; } = string.Empty;

    /// <summary>
    /// 優先順序，數字越小越早處理。
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// 工作狀態。
    /// </summary>
    public BackgroundJobStatus Status { get; set; }

    /// <summary>
    /// 工作狀態名稱。
    /// </summary>
    public string StatusName { get; set; } = string.Empty;

    /// <summary>
    /// 已重試次數。
    /// </summary>
    public int RetryCount { get; set; }

    /// <summary>
    /// 最大重試次數。
    /// </summary>
    public int MaxRetryCount { get; set; }

    /// <summary>
    /// 預計執行時間。
    /// </summary>
    public DateTime ScheduledTime { get; set; }

    /// <summary>
    /// 開始處理時間。
    /// </summary>
    public DateTime? StartedTime { get; set; }

    /// <summary>
    /// 完成時間。
    /// </summary>
    public DateTime? CompletedTime { get; set; }

    /// <summary>
    /// 工作鎖定到期時間。
    /// </summary>
    public DateTime? LockedUntil { get; set; }

    /// <summary>
    /// 目前處理此工作的 worker 識別。
    /// </summary>
    public string LockedBy { get; set; } = string.Empty;

    /// <summary>
    /// 最後一次錯誤訊息。
    /// </summary>
    public string LastError { get; set; } = string.Empty;

    /// <summary>
    /// 建立時間。
    /// </summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>
    /// 建立人員。
    /// </summary>
    public string CreatedId { get; set; } = string.Empty;

    /// <summary>
    /// 更新時間。
    /// </summary>
    public DateTime UpdatedTime { get; set; }

    /// <summary>
    /// 更新人員。
    /// </summary>
    public string UpdatedId { get; set; } = string.Empty;
}
