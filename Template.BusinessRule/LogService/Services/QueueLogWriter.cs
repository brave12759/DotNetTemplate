using System.Text.Json;
using Template.BusinessRule.LogService.Models;
using Template.Common.Enums;
using Template.DataAccess.LogDbContext;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 佇列日誌 writer，專責寫入 QueueLog 資料表
/// </summary>
public class QueueLogWriter(LogDbContext logDb) : ILogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LogWriterTypeEnum LogType => LogWriterTypeEnum.Queue;

    public async Task<long> WriteAsync(object request, CancellationToken cancellationToken = default)
    {
        if (request is not QueueLogCreateRequest logRequest)
            throw new ArgumentException("QueueLogWriter 只接受 QueueLogCreateRequest。", nameof(request));

        var entity = new QueueLog
        {
            EventTime = logRequest.EventTime ?? DateTime.UtcNow,
            OperatorId = Truncate(logRequest.OperatorId, 50),
            JobId = logRequest.JobId,
            WorkType = logRequest.WorkType,
            WorkKey = Truncate(logRequest.WorkKey, 200),
            EventName = Truncate(logRequest.EventName, 50),
            Status = logRequest.Status,
            RetryCount = logRequest.RetryCount,
            Message = logRequest.Message.Trim(),
            ErrorMessage = logRequest.ErrorMessage.Trim(),
            MetadataJson = logRequest.Metadata is null ? string.Empty : JsonSerializer.Serialize(logRequest.Metadata, JsonOptions)
        };

        logDb.QueueLogs.Add(entity);
        await logDb.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    private static string Truncate(string? value, int maxLength)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
