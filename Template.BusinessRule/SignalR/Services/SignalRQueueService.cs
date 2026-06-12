using System.Text.Json;
using Template.Common.BackgroundQueue;
using Template.Common.SignalR;

namespace Template.BusinessRule.SignalR.Services;

/// <summary>
/// SignalR 背景推播佇列服務。
/// </summary>
public sealed class SignalRQueueService(IBackgroundTaskQueue backgroundTaskQueue) : ISignalRQueueService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null
    };

    public Task<long> QueueAllAsync(
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueAsync(
            new SignalRQueuedMessage
            {
                TargetType = SignalRTargetType.All,
                Method = method,
                Payload = payload
            },
            priority,
            cancellationToken);
    }

    public Task<long> QueueGroupAsync(
        string groupName,
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueAsync(
            new SignalRQueuedMessage
            {
                TargetType = SignalRTargetType.Group,
                Target = groupName,
                Method = method,
                Payload = payload
            },
            priority,
            cancellationToken);
    }

    public Task<long> QueueUserAsync(
        string userId,
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueAsync(
            new SignalRQueuedMessage
            {
                TargetType = SignalRTargetType.User,
                Target = userId,
                Method = method,
                Payload = payload
            },
            priority,
            cancellationToken);
    }

    public Task<long> QueueConnectionAsync(
        string connectionId,
        string method,
        object? payload,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        return QueueAsync(
            new SignalRQueuedMessage
            {
                TargetType = SignalRTargetType.Connection,
                Target = connectionId,
                Method = method,
                Payload = payload
            },
            priority,
            cancellationToken);
    }

    public Task<long> QueueAsync(
        SignalRQueuedMessage message,
        int priority = 0,
        CancellationToken cancellationToken = default)
    {
        ValidateMessage(message);

        var payloadJson = JsonSerializer.Serialize(message, SerializerOptions);
        var workKey = BuildWorkKey(message);

        return backgroundTaskQueue.EnqueueAsync(
            BackgroundWorkType.SignalRMessage,
            payloadJson,
            workKey,
            priority,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// 驗證 SignalR 佇列訊息的必要欄位與目標設定。
    /// </summary>
    private static void ValidateMessage(SignalRQueuedMessage message)
    {
        if (!Enum.IsDefined(message.TargetType))
            throw new ArgumentException("SignalR TargetType is invalid.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Method))
            throw new ArgumentException("SignalR Method is required.", nameof(message));

        if (message.TargetType != SignalRTargetType.All && string.IsNullOrWhiteSpace(message.Target))
            throw new ArgumentException("SignalR Target is required for this target type.", nameof(message));
    }

    /// <summary>
    /// 建立背景佇列工作識別鍵，避免相同 SignalR 訊息難以追蹤。
    /// </summary>
    private static string BuildWorkKey(SignalRQueuedMessage message)
    {
        return message.TargetType == SignalRTargetType.All
            ? $"{message.TargetType}:{message.Method}"
            : $"{message.TargetType}:{message.Target}:{message.Method}";
    }
}
