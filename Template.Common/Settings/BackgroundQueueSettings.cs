using Template.Common.BackgroundQueue;

namespace Template.Common.Settings;

/// <summary>
/// 背景工作佇列設定。
/// </summary>
public class BackgroundQueueSettings
{
    /// <summary>
    /// appsettings.json 區段名稱。
    /// </summary>
    public const string SectionName = "BackgroundQueueSettings";

    /// <summary>
    /// 是否啟用背景工作佇列 worker。
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// 預設輪詢間隔秒數。
    /// </summary>
    public int DefaultPollingIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// 預設工作鎖定秒數，超過後可被其他 worker 重新取得。
    /// </summary>
    public int DefaultLockTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// 預設最大重試次數。
    /// </summary>
    public int DefaultMaxRetryCount { get; set; } = 3;

    /// <summary>
    /// 停機時等待 worker 停止的秒數。
    /// </summary>
    public int ShutdownTimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// 各工作類型的 worker 設定。
    /// </summary>
    public List<BackgroundQueueWorkerSettings> Workers { get; set; } = [];
}

/// <summary>
/// 背景工作類型 worker 設定。
/// </summary>
public class BackgroundQueueWorkerSettings
{
    /// <summary>
    /// 工作類型。
    /// </summary>
    public BackgroundWorkType WorkType { get; set; }

    /// <summary>
    /// 此工作類型同時執行的 worker 數量。
    /// </summary>
    public int WorkerCount { get; set; } = 1;

    /// <summary>
    /// 輪詢間隔秒數。未設定時使用預設值。
    /// </summary>
    public int? PollingIntervalSeconds { get; set; }

    /// <summary>
    /// 工作鎖定秒數。未設定時使用預設值。
    /// </summary>
    public int? LockTimeoutSeconds { get; set; }

    /// <summary>
    /// 最大重試次數。未設定時使用預設值。
    /// </summary>
    public int? MaxRetryCount { get; set; }
}
