using System.Text.Json;
using Microsoft.AspNetCore.SignalR;
using Template.Common.BackgroundQueue;
using Template.Common.SignalR;
using Template.WebApi.Hubs;

namespace Template.WebApi.SignalR;

/// <summary>
/// 從背景佇列取出 SignalR 推播訊息並送到 Hub。
/// </summary>
public sealed class QueuedSignalRMessageHandler(
    IHubContext<NotificationHub> hubContext,
    ILogger<QueuedSignalRMessageHandler> logger) : IBackgroundJobHandler
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = null,
        DictionaryKeyPolicy = null
    };

    public BackgroundWorkType WorkType => BackgroundWorkType.SignalRMessage;

    public async Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<SignalRQueuedMessage>(job.PayloadJson, SerializerOptions)
            ?? throw new InvalidOperationException($"SignalR job payload is empty. JobId={job.Id}");

        ValidateMessage(message, job.Id);

        logger.LogInformation(
            "Sending queued SignalR message. JobId={JobId}, TargetType={TargetType}, Target={Target}, Method={Method}",
            job.Id,
            message.TargetType,
            message.Target,
            message.Method);

        switch (message.TargetType)
        {
            case SignalRTargetType.All:
                await hubContext.Clients.All.SendAsync(message.Method, message.Payload, cancellationToken);
                break;

            case SignalRTargetType.Group:
                await hubContext.Clients.Group(message.Target).SendAsync(message.Method, message.Payload, cancellationToken);
                break;

            case SignalRTargetType.User:
                await hubContext.Clients.User(message.Target).SendAsync(message.Method, message.Payload, cancellationToken);
                break;

            case SignalRTargetType.Connection:
                await hubContext.Clients.Client(message.Target).SendAsync(message.Method, message.Payload, cancellationToken);
                break;

            default:
                throw new InvalidOperationException($"Unsupported SignalR TargetType: {message.TargetType}");
        }
    }

    /// <summary>
    /// 驗證 SignalR 佇列訊息的必要欄位與目標設定。
    /// </summary>
    private static void ValidateMessage(SignalRQueuedMessage message, long jobId)
    {
        if (!Enum.IsDefined(message.TargetType))
            throw new InvalidOperationException($"SignalR TargetType is invalid. JobId={jobId}");

        if (string.IsNullOrWhiteSpace(message.Method))
            throw new InvalidOperationException($"SignalR Method is required. JobId={jobId}");

        if (message.TargetType != SignalRTargetType.All && string.IsNullOrWhiteSpace(message.Target))
            throw new InvalidOperationException($"SignalR Target is required. JobId={jobId}");
    }
}
