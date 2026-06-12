using System.Text.Json;
using Template.BusinessRule.LogService.Models;
using Template.Common.Enums;
using Template.DataAccess.LogDbContext;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// SSO 日誌 writer，專責寫入 SsoLog 資料表
/// </summary>
public class SsoLogWriter(LogDbContext logDb) : ILogWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public LogWriterTypeEnum LogType => LogWriterTypeEnum.Sso;

    public async Task<long> WriteAsync(object request, CancellationToken cancellationToken = default)
    {
        if (request is not SsoLogCreateRequest logRequest)
            throw new ArgumentException("SsoLogWriter 只接受 SsoLogCreateRequest。", nameof(request));

        var entity = new SsoLog
        {
            EventTime = logRequest.EventTime ?? DateTime.UtcNow,
            OperatorId = Truncate(logRequest.OperatorId, 100),
            ClientId = Truncate(logRequest.ClientId, 100),
            EventName = Truncate(logRequest.EventName, 50),
            Result = Truncate(logRequest.Result, 20),
            IpAddress = Truncate(logRequest.IpAddress, 50),
            Message = logRequest.Message.Trim(),
            MetadataJson = logRequest.Metadata is null ? string.Empty : JsonSerializer.Serialize(logRequest.Metadata, JsonOptions)
        };

        logDb.SsoLogs.Add(entity);
        await logDb.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    private static string Truncate(string? value, int maxLength)
    {
        var text = value?.Trim() ?? string.Empty;
        return text.Length <= maxLength ? text : text[..maxLength];
    }
}
