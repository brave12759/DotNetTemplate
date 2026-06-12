namespace Template.Common.SignalR;

/// <summary>
/// 將 SignalR 推播訊息寫入背景佇列，讓 worker 非同步送出。
/// </summary>
public interface ISignalRQueueService
{
    /// <summary>
    /// 將訊息推播給所有 client。
    /// </summary>
    Task<long> QueueAllAsync(
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 將訊息推播給指定群組。
    /// </summary>
    Task<long> QueueGroupAsync(
        string groupName,
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 將訊息推播給指定使用者。
    /// </summary>
    Task<long> QueueUserAsync(
        string userId,
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 將訊息推播給指定連線。
    /// </summary>
    Task<long> QueueConnectionAsync(
        string connectionId,
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 使用完整訊息物件寫入背景佇列。
    /// </summary>
    Task<long> QueueAsync(
        SignalRQueuedMessage message,
        int priority = 0,
        CancellationToken cancellationToken = default);
}
