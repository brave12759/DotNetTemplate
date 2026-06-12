using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Template.BusinessRule;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.BackgroundQueue;
using Template.Common.Settings;
using Template.DataAccess.ProjectDbContext;

namespace Template.BusinessRule.BackgroundQueue.Services;

/// <summary>
/// 資料庫背景工作佇列。
/// </summary>
public class DbBackgroundTaskQueue(
    IServiceProvider serviceProvider,
    BackgroundQueueSettings settings) : BaseService(serviceProvider), IBackgroundTaskQueue
{
    private readonly Lazy<ILogService?> _logService = new(() => serviceProvider.GetService<ILogService>());

    public async Task<long> EnqueueAsync(
        BackgroundWorkType workType,
        string payloadJson = "",
        string? workKey = null,
        int priority = 0,
        DateTime? scheduledTime = null,
        int? maxRetryCount = null,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(workType))
            throw new ArgumentException("WorkType 不在定義範圍內。", nameof(workType));

        var now = DateTime.UtcNow;
        var userId = "system";
        try
        {
            if (!string.IsNullOrWhiteSpace(CurrentUser.UserId))
                userId = CurrentUser.UserId;
        }
        catch
        {
            userId = "system";
        }

        var entity = new Sys_BackgroundJob
        {
            WorkType = (int)workType,
            WorkKey = workKey?.Trim() ?? string.Empty,
            PayloadJson = payloadJson,
            LastError = string.Empty,
            Priority = priority,
            Status = (int)BackgroundJobStatus.Pending,
            RetryCount = 0,
            MaxRetryCount = maxRetryCount ?? settings.DefaultMaxRetryCount,
            ScheduledTime = scheduledTime ?? now,
            LockedBy = string.Empty,
            CreatedTime = now,
            CreatedId = userId,
            UpdatedTime = now,
            UpdatedId = userId,
            Version = Guid.NewGuid()
        };

        Db.Sys_BackgroundJobs.Add(entity);
        await Db.SaveChangesAsync(cancellationToken);
        await WriteQueueLogAsync(
            entity,
            "Enqueue",
            userId,
            "建立背景工作。",
            cancellationToken: cancellationToken);
        return entity.Id;
    }

    public async Task<BackgroundJob?> TryClaimNextAsync(
        BackgroundWorkType workType,
        string workerId,
        TimeSpan lockTimeout,
        CancellationToken cancellationToken = default)
    {
        if (!Enum.IsDefined(workType))
            throw new ArgumentException("WorkType 不在定義範圍內。", nameof(workType));

        var now = DateTime.UtcNow;

        for (var attempt = 0; attempt < 3; attempt++)
        {
            var entity = await Db.Sys_BackgroundJobs
                .Where(j =>
                    j.WorkType == (int)workType &&
                    ((j.Status == (int)BackgroundJobStatus.Pending &&
                        j.ScheduledTime <= now) ||
                     (j.Status == (int)BackgroundJobStatus.Processing &&
                        j.LockedUntil <= now)))
                .OrderBy(j => j.Priority)
                .ThenBy(j => j.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (entity is null)
                return null;

            entity.Status = (int)BackgroundJobStatus.Processing;
            entity.StartedTime = now;
            entity.LockedUntil = now.Add(lockTimeout);
            entity.LockedBy = workerId;
            entity.UpdatedTime = now;
            entity.UpdatedId = workerId;
            entity.Version = Guid.NewGuid();

            try
            {
                await Db.SaveChangesAsync(cancellationToken);
                await WriteQueueLogAsync(
                    entity,
                    "Claim",
                    workerId,
                    "背景工作已被執行者領取。",
                    cancellationToken: cancellationToken);
                return Map(entity);
            }
            catch (DbUpdateConcurrencyException)
            {
                Db.ChangeTracker.Clear();
            }
        }

        return null;
    }

    public async Task CompleteAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await Db.Sys_BackgroundJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (entity is null)
            return;

        entity.Status = (int)BackgroundJobStatus.Succeeded;
        entity.CompletedTime = now;
        entity.LockedUntil = null;
        entity.UpdatedTime = now;
        entity.UpdatedId = entity.LockedBy;
        entity.Version = Guid.NewGuid();

        await Db.SaveChangesAsync(cancellationToken);
        await WriteQueueLogAsync(
            entity,
            "Complete",
            entity.LockedBy,
            "背景工作執行成功。",
            cancellationToken: cancellationToken);
    }

    public async Task FailAsync(
        long jobId,
        string errorMessage,
        DateTime? nextRunTime,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var entity = await Db.Sys_BackgroundJobs.FirstOrDefaultAsync(j => j.Id == jobId, cancellationToken);
        if (entity is null)
            return;

        entity.RetryCount++;
        entity.LastError = errorMessage.Length > 4000 ? errorMessage[..4000] : errorMessage;
        entity.LockedUntil = null;
        entity.UpdatedTime = now;
        entity.UpdatedId = entity.LockedBy;
        entity.Version = Guid.NewGuid();

        if (entity.RetryCount >= entity.MaxRetryCount)
        {
            entity.Status = (int)BackgroundJobStatus.Failed;
            entity.CompletedTime = now;
        }
        else
        {
            entity.Status = (int)BackgroundJobStatus.Pending;
            entity.ScheduledTime = nextRunTime ?? now.AddSeconds(30);
        }

        await Db.SaveChangesAsync(cancellationToken);
        await WriteQueueLogAsync(
            entity,
            "Fail",
            entity.LockedBy,
            entity.Status == (int)BackgroundJobStatus.Failed
                ? "背景工作執行失敗且已達重試上限。"
                : "背景工作執行失敗，等待下次重試。",
            errorMessage,
            cancellationToken);
    }

    public async Task<int> CountPendingAsync(BackgroundWorkType? workType = null, CancellationToken cancellationToken = default)
    {
        var query = Db.Sys_BackgroundJobs
            .AsNoTracking()
            .Where(j => j.Status == (int)BackgroundJobStatus.Pending);

        if (workType.HasValue)
            query = query.Where(j => j.WorkType == (int)workType.Value);

        return await query.CountAsync(cancellationToken);
    }

    /// <summary>
    /// 將背景工作資料表實體轉成背景佇列領域模型。
    /// </summary>
    private static BackgroundJob Map(Sys_BackgroundJob entity)
    {
        return new BackgroundJob
        {
            Id = entity.Id,
            WorkType = (BackgroundWorkType)entity.WorkType,
            WorkKey = entity.WorkKey,
            PayloadJson = entity.PayloadJson,
            Priority = entity.Priority,
            Status = (BackgroundJobStatus)entity.Status,
            RetryCount = entity.RetryCount,
            MaxRetryCount = entity.MaxRetryCount,
            ScheduledTime = entity.ScheduledTime,
            CreatedTime = entity.CreatedTime
        };
    }

    /// <summary>
    /// 寫入佇列日誌；測試未註冊 LogService 時略過。
    /// </summary>
    private Task WriteQueueLogAsync(
        Sys_BackgroundJob entity,
        string eventName,
        string operatorId,
        string message,
        string errorMessage = "",
        CancellationToken cancellationToken = default)
    {
        return _logService.Value?.WriteQueueAsync(new QueueLogCreateRequest
        {
            OperatorId = operatorId,
            JobId = entity.Id,
            WorkType = entity.WorkType,
            WorkKey = entity.WorkKey,
            EventName = eventName,
            Status = entity.Status,
            RetryCount = entity.RetryCount,
            Message = message,
            ErrorMessage = errorMessage,
            Metadata = new
            {
                entity.ScheduledTime,
                entity.StartedTime,
                entity.CompletedTime,
                entity.LockedBy
            }
        }, cancellationToken) ?? Task.CompletedTask;
    }
}
