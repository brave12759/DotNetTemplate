using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Template.Common.BackgroundQueue;
using Template.Common.Settings;

namespace Template.BusinessRule.BackgroundQueue.Services;

/// <summary>
/// 背景佇列執行服務。
/// </summary>
public sealed class QueuedBackgroundService(
    IServiceScopeFactory serviceScopeFactory,
    BackgroundQueueSettings settings,
    ILogger<QueuedBackgroundService> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!settings.Enabled)
        {
            logger.LogInformation("背景資料庫佇列 worker 未啟用。");
            return;
        }

        using var scope = serviceScopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IBackgroundJobHandler>().ToList();
        if (handlers.Count == 0)
        {
            logger.LogInformation("未註冊任何背景工作處理器，背景資料庫佇列 worker 不啟動。");
            return;
        }

        var workers = handlers
            .SelectMany(handler =>
            {
                var workerSettings = GetWorkerSettings(handler.WorkType);
                var workerCount = Math.Max(1, workerSettings.WorkerCount);
                return Enumerable
                    .Range(1, workerCount)
                    .Select(workerNo => RunWorkerAsync(handler.WorkType, workerNo, workerSettings, stoppingToken));
            })
            .ToArray();

        await Task.WhenAll(workers);
    }

    /// <inheritdoc />
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        var timeoutSeconds = Math.Max(1, settings.ShutdownTimeoutSeconds);
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        await base.StopAsync(linkedCts.Token);
    }

    private async Task RunWorkerAsync(
        BackgroundWorkType workType,
        int workerNo,
        BackgroundQueueWorkerSettings workerSettings,
        CancellationToken stoppingToken)
    {
        var workerId = $"{Environment.MachineName}:{workType}:{workerNo}:{Guid.NewGuid():N}";
        var pollingInterval = TimeSpan.FromSeconds(workerSettings.PollingIntervalSeconds ?? settings.DefaultPollingIntervalSeconds);
        var lockTimeout = TimeSpan.FromSeconds(workerSettings.LockTimeoutSeconds ?? settings.DefaultLockTimeoutSeconds);

        logger.LogInformation("背景資料庫佇列 Worker 已啟動。WorkType={WorkType}, WorkerId={WorkerId}", workType, workerId);

        while (!stoppingToken.IsCancellationRequested)
        {
            using var scope = serviceScopeFactory.CreateScope();
            var queue = scope.ServiceProvider.GetRequiredService<IBackgroundTaskQueue>();
            var handler = scope.ServiceProvider
                .GetServices<IBackgroundJobHandler>()
                .FirstOrDefault(h => h.WorkType == workType);

            if (handler is null)
            {
                await Task.Delay(pollingInterval, stoppingToken);
                continue;
            }

            var job = await queue.TryClaimNextAsync(workType, workerId, lockTimeout, stoppingToken);
            if (job is null)
            {
                await Task.Delay(pollingInterval, stoppingToken);
                continue;
            }

            await ExecuteJobAsync(queue, handler, workerId, job, workerSettings, stoppingToken);
        }

        logger.LogInformation("背景資料庫佇列 Worker 已停止。WorkType={WorkType}, WorkerId={WorkerId}", workType, workerId);
    }

    private async Task ExecuteJobAsync(
        IBackgroundTaskQueue queue,
        IBackgroundJobHandler handler,
        string workerId,
        BackgroundJob job,
        BackgroundQueueWorkerSettings workerSettings,
        CancellationToken stoppingToken)
    {
        var startedTime = DateTime.UtcNow;
        logger.LogInformation(
            "背景工作開始。WorkerId={WorkerId}, JobId={JobId}, WorkType={WorkType}",
            workerId,
            job.Id,
            job.WorkType);

        try
        {
            await handler.HandleAsync(job, stoppingToken);
            await queue.CompleteAsync(job.Id, stoppingToken);

            logger.LogInformation(
                "背景工作完成。WorkerId={WorkerId}, JobId={JobId}, WorkType={WorkType}, ElapsedMs={ElapsedMs}",
                workerId,
                job.Id,
                job.WorkType,
                (DateTime.UtcNow - startedTime).TotalMilliseconds);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "背景工作因應用程式停止而取消。WorkerId={WorkerId}, JobId={JobId}, WorkType={WorkType}",
                workerId,
                job.Id,
                job.WorkType);
        }
        catch (Exception ex)
        {
            var retryDelaySeconds = Math.Min(3600, Math.Pow(2, job.RetryCount) * 30);
            await queue.FailAsync(
                job.Id,
                ex.ToString(),
                DateTime.UtcNow.AddSeconds(retryDelaySeconds),
                CancellationToken.None);

            logger.LogError(
                ex,
                "背景工作執行失敗。WorkerId={WorkerId}, JobId={JobId}, WorkType={WorkType}",
                workerId,
                job.Id,
                job.WorkType);
        }
    }

    /// <summary>
    /// 依工作類型取得背景工作執行設定，沒有專屬設定時使用預設值。
    /// </summary>
    private BackgroundQueueWorkerSettings GetWorkerSettings(BackgroundWorkType workType)
    {
        var configured = settings.Workers.FirstOrDefault(w =>
            w.WorkType == workType);

        return configured ?? new BackgroundQueueWorkerSettings
        {
            WorkType = workType,
            WorkerCount = 1,
            PollingIntervalSeconds = settings.DefaultPollingIntervalSeconds,
            LockTimeoutSeconds = settings.DefaultLockTimeoutSeconds,
            MaxRetryCount = settings.DefaultMaxRetryCount
        };
    }
}
