namespace Template.Common.BackgroundQueue;

/// <summary>
/// 背景工作資料。
/// </summary>
public class BackgroundJob
{
    public long Id { get; set; }

    public BackgroundWorkType WorkType { get; set; }

    public string WorkKey { get; set; } = string.Empty;

    public string PayloadJson { get; set; } = string.Empty;

    public int Priority { get; set; }

    public BackgroundJobStatus Status { get; set; }

    public int RetryCount { get; set; }

    public int MaxRetryCount { get; set; }

    public DateTime ScheduledTime { get; set; }

    public DateTime CreatedTime { get; set; }
}
