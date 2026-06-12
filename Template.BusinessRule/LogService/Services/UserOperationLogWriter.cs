using System.Text.Json;
using Template.BusinessRule.LogService.Models;
using Template.Common.Enums;
using Template.DataAccess.LogDbContext;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 使用者操作日誌 writer，專責寫入 UserOperationLog 資料表。
/// </summary>
public class UserOperationLogWriter(LogDbContext logDb) : ILogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <inheritdoc />
    public LogWriterTypeEnum LogType => LogWriterTypeEnum.UserOperation;

    /// <inheritdoc />
    public async Task<long> WriteAsync(object request, CancellationToken cancellationToken = default)
    {
        if (request is not UserOperationLogCreateRequest logRequest)
            throw new ArgumentException("UserOperationLogWriter 只接受 UserOperationLogCreateRequest。", nameof(request));

        var entity = new UserOperationLog
        {
            EventTime = logRequest.EventTime ?? DateTime.UtcNow,
            UserId = Truncate(logRequest.UserId, 50),
            Module = Truncate(logRequest.Module, 100),
            Action = (int)logRequest.Action,
            Result = (int)logRequest.Result,
            TargetType = Truncate(logRequest.TargetType, 100),
            TargetId = Truncate(logRequest.TargetId, 200),
            IpAddress = Truncate(logRequest.IpAddress, 50),
            TraceId = Truncate(logRequest.TraceId, 100),
            Message = logRequest.Message.Trim(),
            OldValueJson = Serialize(logRequest.OldValue),
            NewValueJson = Serialize(logRequest.NewValue),
            MetadataJson = Serialize(logRequest.Metadata)
        };

        logDb.UserOperationLogs.Add(entity);
        await logDb.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    /// <summary>
    /// 物件轉 JSON；未提供資料時保持空字串。
    /// </summary>
    private static string Serialize(object? value)
    {
        return value is null ? string.Empty : JsonSerializer.Serialize(value, JsonOptions);
    }

    /// <summary>
    /// 配合資料庫欄位長度截斷字串，避免日誌寫入因描述過長失敗。
    /// </summary>
    private static string Truncate(string? value, int maxLength)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
