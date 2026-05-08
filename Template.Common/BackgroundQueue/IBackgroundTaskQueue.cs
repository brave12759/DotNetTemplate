namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作資料庫佇列。
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// 將背景工作加入佇列。
    /// </summary>
    Task<long> EnqueueAsync(
        BackgroundWorkType workType,
        string payloadJson = "",
        string? workKey = null,
        int priority = 0,
        DateTime? scheduledTime = null,
        int? maxRetryCount = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 嘗試取得並鎖定下一筆待執行工作。
    /// </summary>
    Task<BackgroundJob?> TryClaimNextAsync(
        BackgroundWorkType workType,
        string workerId,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 標記工作完成。
    /// </summary>
    Task CompleteAsync(long jobId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 標記工作失敗，仍可重試時會重新排程。
    /// </summary>
    Task FailAsync(
        long jobId,
        string errorMessage,
        DateTime? nextRunTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 取得指定工作類型的待處理數量。
    /// </summary>
    Task<int> CountPendingAsync(BackgroundWorkType? workType = null, CancellationToken cancellationToken = default);
}
