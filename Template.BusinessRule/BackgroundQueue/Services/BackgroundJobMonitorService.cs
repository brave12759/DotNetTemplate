using Microsoft.EntityFrameworkCore;
using Template.BusinessRule;
using Template.Common.BackgroundQueue;

namespace Template.BusinessRule.BackgroundQueue.Services;

/// <summary>
/// 背景工作佇列查詢服務。
/// </summary>
public class BackgroundJobMonitorService(IServiceProvider serviceProvider)
    : BaseService(serviceProvider), IBackgroundJobMonitorService
{
    public async Task<BackgroundJobSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var rows = await Db.Sys_BackgroundJobs
            .AsNoTracking()
            .GroupBy(j => new { j.WorkType, j.Status })
            .Select(g => new
            {
                g.Key.WorkType,
                g.Key.Status,
                Count = g.Count()
            })
            .ToListAsync(cancellationToken);

        return new BackgroundJobSummaryDto
        {
            PendingCount = rows.Where(x => x.Status == (int)BackgroundJobStatus.Pending).Sum(x => x.Count),
            ProcessingCount = rows.Where(x => x.Status == (int)BackgroundJobStatus.Processing).Sum(x => x.Count),
            SucceededCount = rows.Where(x => x.Status == (int)BackgroundJobStatus.Succeeded).Sum(x => x.Count),
            FailedCount = rows.Where(x => x.Status == (int)BackgroundJobStatus.Failed).Sum(x => x.Count),
            CanceledCount = rows.Where(x => x.Status == (int)BackgroundJobStatus.Canceled).Sum(x => x.Count),
            WorkTypes = Enum.GetValues<BackgroundWorkType>()
                .Select(workType => new BackgroundJobWorkTypeSummaryDto
                {
                    WorkType = workType,
                    WorkTypeName = workType.ToString(),
                    PendingCount = rows
                        .Where(x => x.WorkType == (int)workType && x.Status == (int)BackgroundJobStatus.Pending)
                        .Sum(x => x.Count),
                    ProcessingCount = rows
                        .Where(x => x.WorkType == (int)workType && x.Status == (int)BackgroundJobStatus.Processing)
                        .Sum(x => x.Count),
                    SucceededCount = rows
                        .Where(x => x.WorkType == (int)workType && x.Status == (int)BackgroundJobStatus.Succeeded)
                        .Sum(x => x.Count),
                    FailedCount = rows
                        .Where(x => x.WorkType == (int)workType && x.Status == (int)BackgroundJobStatus.Failed)
                        .Sum(x => x.Count),
                    CanceledCount = rows
                        .Where(x => x.WorkType == (int)workType && x.Status == (int)BackgroundJobStatus.Canceled)
                        .Sum(x => x.Count)
                })
                .ToList()
        };
    }

    public async Task<BackgroundJobQueryResult> GetListAsync(
        BackgroundWorkType? workType = null,
        BackgroundJobStatus? status = null,
        int page = 1,
        int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (workType.HasValue && !Enum.IsDefined(workType.Value))
            throw new ArgumentException("WorkType 不是有效的背景工作類型。", nameof(workType));

        if (status.HasValue && !Enum.IsDefined(status.Value))
            throw new ArgumentException("Status 不是有效的背景工作狀態。", nameof(status));

        if (page < 1)
            throw new ArgumentException("Page 必須大於 0。", nameof(page));

        if (pageSize is < 1 or > 200)
            throw new ArgumentException("PageSize 必須介於 1 到 200。", nameof(pageSize));

        var query = Db.Sys_BackgroundJobs.AsNoTracking();

        if (workType.HasValue)
            query = query.Where(j => j.WorkType == (int)workType.Value);

        if (status.HasValue)
            query = query.Where(j => j.Status == (int)status.Value);

        var totalCount = await query.CountAsync(cancellationToken);
        var rows = await query
            .OrderByDescending(j => j.CreatedTime)
            .ThenByDescending(j => j.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(j => new
            {
                j.Id,
                j.WorkType,
                j.WorkKey,
                j.PayloadJson,
                j.Priority,
                j.Status,
                j.RetryCount,
                j.MaxRetryCount,
                j.ScheduledTime,
                j.StartedTime,
                j.CompletedTime,
                j.LockedUntil,
                j.LockedBy,
                j.LastError,
                j.CreatedTime,
                j.CreatedId,
                j.UpdatedTime,
                j.UpdatedId
            })
            .ToListAsync(cancellationToken);

        return new BackgroundJobQueryResult
        {
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            Items = rows.Select(j => new BackgroundJobDto
            {
                Id = j.Id,
                WorkType = (BackgroundWorkType)j.WorkType,
                WorkTypeName = ((BackgroundWorkType)j.WorkType).ToString(),
                WorkKey = j.WorkKey,
                PayloadJson = j.PayloadJson,
                Priority = j.Priority,
                Status = (BackgroundJobStatus)j.Status,
                StatusName = ((BackgroundJobStatus)j.Status).ToString(),
                RetryCount = j.RetryCount,
                MaxRetryCount = j.MaxRetryCount,
                ScheduledTime = j.ScheduledTime,
                StartedTime = j.StartedTime,
                CompletedTime = j.CompletedTime,
                LockedUntil = j.LockedUntil,
                LockedBy = j.LockedBy,
                LastError = j.LastError,
                CreatedTime = j.CreatedTime,
                CreatedId = j.CreatedId,
                UpdatedTime = j.UpdatedTime,
                UpdatedId = j.UpdatedId
            }).ToList()
        };
    }

    public async Task<BackgroundJobDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
            throw new ArgumentException("Id 必須大於 0。", nameof(id));

        var row = await Db.Sys_BackgroundJobs
            .AsNoTracking()
            .Where(j => j.Id == id)
            .Select(j => new
            {
                j.Id,
                j.WorkType,
                j.WorkKey,
                j.PayloadJson,
                j.Priority,
                j.Status,
                j.RetryCount,
                j.MaxRetryCount,
                j.ScheduledTime,
                j.StartedTime,
                j.CompletedTime,
                j.LockedUntil,
                j.LockedBy,
                j.LastError,
                j.CreatedTime,
                j.CreatedId,
                j.UpdatedTime,
                j.UpdatedId
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (row is null)
            return null;

        return new BackgroundJobDto
        {
            Id = row.Id,
            WorkType = (BackgroundWorkType)row.WorkType,
            WorkTypeName = ((BackgroundWorkType)row.WorkType).ToString(),
            WorkKey = row.WorkKey,
            PayloadJson = row.PayloadJson,
            Priority = row.Priority,
            Status = (BackgroundJobStatus)row.Status,
            StatusName = ((BackgroundJobStatus)row.Status).ToString(),
            RetryCount = row.RetryCount,
            MaxRetryCount = row.MaxRetryCount,
            ScheduledTime = row.ScheduledTime,
            StartedTime = row.StartedTime,
            CompletedTime = row.CompletedTime,
            LockedUntil = row.LockedUntil,
            LockedBy = row.LockedBy,
            LastError = row.LastError,
            CreatedTime = row.CreatedTime,
            CreatedId = row.CreatedId,
            UpdatedTime = row.UpdatedTime,
            UpdatedId = row.UpdatedId
        };
    }
}
