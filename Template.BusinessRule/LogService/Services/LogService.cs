using Microsoft.EntityFrameworkCore;
using Template.BusinessRule.Extensions;
using Template.BusinessRule.LogService.Models;
using Template.Common.Enums;

namespace Template.BusinessRule.LogService.Services;

/// <summary>
/// 日誌服務入口，負責補齊共用欄位並透過工廠委派給對應 writer
/// </summary>
public class LogService(IServiceProvider serviceProvider, ILogWriterFactory writerFactory)
    : BaseService(serviceProvider), ILogService
{
    /// <inheritdoc />
    public Task<long> WriteUserOperationAsync(
        UserOperationLogCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Module))
            throw new ArgumentException("Module 為必填。", nameof(request));

        if (!Enum.IsDefined(request.Action))
            throw new ArgumentException("Action 不是有效的操作種類。", nameof(request));

        if (!Enum.IsDefined(request.Result))
            throw new ArgumentException("Result 不是有效的執行結果。", nameof(request));

        request.UserId = string.IsNullOrWhiteSpace(request.UserId)
            ? CurrentUserService?.CurrentUser?.UserId ?? "system"
            : request.UserId.Trim();

        request.EventTime ??= DateTime.UtcNow;

        var writer = writerFactory.Create(LogWriterTypeEnum.UserOperation);
        return writer.WriteAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(
        UserOperationLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        PageListQueryableExtensions.ValidatePaging(request.Page, request.PageSize);

        var query = LogDb.UserOperationLogs.AsNoTracking();

        if (request.StartTime.HasValue)
            query = query.Where(x => x.EventTime >= request.StartTime.Value);

        if (request.EndTime.HasValue)
            query = query.Where(x => x.EventTime <= request.EndTime.Value);

        if (!string.IsNullOrWhiteSpace(request.UserId))
            query = query.Where(x => x.UserId == request.UserId.Trim());

        if (!string.IsNullOrWhiteSpace(request.Module))
            query = query.Where(x => x.Module == request.Module.Trim());

        if (request.Action.HasValue)
            query = query.Where(x => x.Action == (int)request.Action.Value);

        if (request.Result.HasValue)
            query = query.Where(x => x.Result == (int)request.Result.Value);

        if (!string.IsNullOrWhiteSpace(request.TargetType))
            query = query.Where(x => x.TargetType == request.TargetType.Trim());

        if (!string.IsNullOrWhiteSpace(request.TargetId))
            query = query.Where(x => x.TargetId == request.TargetId.Trim());

        var pageOutput = await query
            .OrderByDescending(x => x.EventTime)
            .ThenByDescending(x => x.Id)
            .Select(x => new UserOperationLogDto
            {
                Id = x.Id,
                EventTime = x.EventTime,
                UserId = x.UserId,
                Module = x.Module,
                Action = (AuditActionEnum)x.Action,
                ActionName = ((AuditActionEnum)x.Action).ToString(),
                Result = (AuditResultEnum)x.Result,
                ResultName = ((AuditResultEnum)x.Result).ToString(),
                TargetType = x.TargetType,
                TargetId = x.TargetId,
                IpAddress = x.IpAddress,
                TraceId = x.TraceId,
                Message = x.Message,
                OldValueJson = x.OldValueJson,
                NewValueJson = x.NewValueJson,
                MetadataJson = x.MetadataJson
            })
            .ToPageListOutputAsync(request.Page, request.PageSize, enablePaging: true, cancellationToken);

        return new UserOperationLogQueryResult
        {
            TotalCount = pageOutput.TotalCount,
            Page = pageOutput.Page,
            PageSize = pageOutput.PageSize,
            Items = pageOutput.Items
        };
    }

    /// <inheritdoc />
    public Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.OperatorId = NormalizeOperatorId(request.OperatorId);
        request.EventTime ??= DateTime.UtcNow;
        return writerFactory.Create(LogWriterTypeEnum.Queue).WriteAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<QueueLogQueryResult> GetQueueLogsAsync(
        QueueLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        PageListQueryableExtensions.ValidatePaging(request.Page, request.PageSize);

        var query = LogDb.QueueLogs.AsNoTracking();
        if (request.StartTime.HasValue)
            query = query.Where(x => x.EventTime >= request.StartTime.Value);
        if (request.EndTime.HasValue)
            query = query.Where(x => x.EventTime <= request.EndTime.Value);
        if (!string.IsNullOrWhiteSpace(request.OperatorId))
            query = query.Where(x => x.OperatorId == request.OperatorId.Trim());

        var pageOutput = await query
            .OrderByDescending(x => x.EventTime)
            .ThenByDescending(x => x.Id)
            .Select(x => new QueueLogDto
            {
                Id = x.Id,
                EventTime = x.EventTime,
                OperatorId = x.OperatorId,
                JobId = x.JobId,
                WorkType = x.WorkType,
                WorkKey = x.WorkKey,
                EventName = x.EventName,
                Status = x.Status,
                RetryCount = x.RetryCount,
                Message = x.Message,
                ErrorMessage = x.ErrorMessage,
                MetadataJson = x.MetadataJson
            })
            .ToPageListOutputAsync(request.Page, request.PageSize, enablePaging: true, cancellationToken);

        return new QueueLogQueryResult
        {
            TotalCount = pageOutput.TotalCount,
            Page = pageOutput.Page,
            PageSize = pageOutput.PageSize,
            Items = pageOutput.Items
        };
    }

    /// <inheritdoc />
    public Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        request.OperatorId = NormalizeOperatorId(request.OperatorId);
        request.EventTime ??= DateTime.UtcNow;
        return writerFactory.Create(LogWriterTypeEnum.Sso).WriteAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<SsoLogQueryResult> GetSsoLogsAsync(
        SsoLogQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        PageListQueryableExtensions.ValidatePaging(request.Page, request.PageSize);

        var query = LogDb.SsoLogs.AsNoTracking();
        if (request.StartTime.HasValue)
            query = query.Where(x => x.EventTime >= request.StartTime.Value);
        if (request.EndTime.HasValue)
            query = query.Where(x => x.EventTime <= request.EndTime.Value);
        if (!string.IsNullOrWhiteSpace(request.OperatorId))
            query = query.Where(x => x.OperatorId == request.OperatorId.Trim());

        var pageOutput = await query
            .OrderByDescending(x => x.EventTime)
            .ThenByDescending(x => x.Id)
            .Select(x => new SsoLogDto
            {
                Id = x.Id,
                EventTime = x.EventTime,
                OperatorId = x.OperatorId,
                ClientId = x.ClientId,
                EventName = x.EventName,
                Result = x.Result,
                IpAddress = x.IpAddress,
                Message = x.Message,
                MetadataJson = x.MetadataJson
            })
            .ToPageListOutputAsync(request.Page, request.PageSize, enablePaging: true, cancellationToken);

        return new SsoLogQueryResult
        {
            TotalCount = pageOutput.TotalCount,
            Page = pageOutput.Page,
            PageSize = pageOutput.PageSize,
            Items = pageOutput.Items
        };
    }

    /// <summary>
    /// 標準化操作者代碼；沒有登入者時以 system 表示。
    /// </summary>
    private string NormalizeOperatorId(string operatorId)
    {
        return string.IsNullOrWhiteSpace(operatorId)
            ? CurrentUserService?.CurrentUser?.UserId ?? "system"
            : operatorId.Trim();
    }
}
