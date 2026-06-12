using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.BackgroundQueue.Services;
using Template.Common.BackgroundQueue;
using Template.Common.Models;
using Template.Common.Services;
using Template.Common.Settings;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class BackgroundTaskQueueTests
{
    [TestMethod]
    public async Task EnqueueAsync_Should_Create_DatabaseJob()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);

        var id = await queue.EnqueueAsync(BackgroundWorkType.Report, """{"ReportId":1}""", workKey: "RPT-1");

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var job = await db.Sys_BackgroundJobs.FirstAsync(x => x.Id == id);

        Assert.AreEqual((int)BackgroundWorkType.Report, job.WorkType);
        Assert.AreEqual("RPT-1", job.WorkKey);
        Assert.AreEqual("""{"ReportId":1}""", job.PayloadJson);
        Assert.AreEqual(0, job.Status);
        Assert.AreEqual("tester", job.CreatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.QueueLogs.Count);
        Assert.AreEqual("Enqueue", logService.QueueLogs[0].EventName);
        Assert.AreEqual(id, logService.QueueLogs[0].JobId);
    }

    [TestMethod]
    public async Task TryClaimNextAsync_Should_Lock_FirstPendingJob_ByWorkType()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);

        await queue.EnqueueAsync(BackgroundWorkType.AttachmentUpload, """{"FileId":1}""", priority: 10);
        var firstId = await queue.EnqueueAsync(BackgroundWorkType.Report, """{"ReportId":1}""", priority: 5);
        await queue.EnqueueAsync(BackgroundWorkType.Report, """{"ReportId":2}""", priority: 20);

        var job = await queue.TryClaimNextAsync(BackgroundWorkType.Report, "worker-1", TimeSpan.FromMinutes(5));

        Assert.IsNotNull(job);
        Assert.AreEqual(firstId, job.Id);
        Assert.AreEqual(BackgroundWorkType.Report, job.WorkType);

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_BackgroundJobs.FirstAsync(x => x.Id == firstId);
        Assert.AreEqual(1, entity.Status);
        Assert.AreEqual("worker-1", entity.LockedBy);
        Assert.IsNotNull(entity.LockedUntil);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.IsTrue(logService.QueueLogs.Any(x => x.EventName == "Claim" && x.OperatorId == "worker-1"));
    }

    [TestMethod]
    public async Task TryClaimNextAsync_Should_Reclaim_ExpiredProcessingJob()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var now = DateTime.UtcNow;
        var entity = new Sys_BackgroundJob
        {
            WorkType = (int)BackgroundWorkType.Report,
            PayloadJson = "",
            Priority = 0,
            Status = (int)BackgroundJobStatus.Processing,
            RetryCount = 0,
            MaxRetryCount = 3,
            ScheduledTime = now.AddMinutes(-10),
            StartedTime = now.AddMinutes(-5),
            LockedUntil = now.AddMinutes(-1),
            LockedBy = "worker-crashed",
            CreatedTime = now.AddMinutes(-10),
            CreatedId = "tester",
            UpdatedTime = now.AddMinutes(-5),
            UpdatedId = "worker-crashed",
            Version = Guid.NewGuid()
        };
        db.Sys_BackgroundJobs.Add(entity);
        await db.SaveChangesAsync();

        var job = await queue.TryClaimNextAsync(BackgroundWorkType.Report, "worker-2", TimeSpan.FromMinutes(5));

        Assert.IsNotNull(job);
        Assert.AreEqual(entity.Id, job.Id);
        Assert.AreEqual(BackgroundJobStatus.Processing, job.Status);

        await db.Entry(entity).ReloadAsync();
        Assert.AreEqual((int)BackgroundJobStatus.Processing, entity.Status);
        Assert.AreEqual("worker-2", entity.LockedBy);
        Assert.IsTrue(entity.LockedUntil > now);
        Assert.IsTrue(entity.StartedTime > now);
    }

    [TestMethod]
    public async Task TryClaimNextAsync_Should_NotClaim_ActiveProcessingJob()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var now = DateTime.UtcNow;
        var entity = new Sys_BackgroundJob
        {
            WorkType = (int)BackgroundWorkType.Report,
            PayloadJson = "",
            Priority = 0,
            Status = (int)BackgroundJobStatus.Processing,
            RetryCount = 0,
            MaxRetryCount = 3,
            ScheduledTime = now.AddMinutes(-10),
            StartedTime = now.AddMinutes(-1),
            LockedUntil = now.AddMinutes(5),
            LockedBy = "worker-active",
            CreatedTime = now.AddMinutes(-10),
            CreatedId = "tester",
            UpdatedTime = now.AddMinutes(-1),
            UpdatedId = "worker-active",
            Version = Guid.NewGuid()
        };
        db.Sys_BackgroundJobs.Add(entity);
        await db.SaveChangesAsync();

        var job = await queue.TryClaimNextAsync(BackgroundWorkType.Report, "worker-2", TimeSpan.FromMinutes(5));

        Assert.IsNull(job);

        await db.Entry(entity).ReloadAsync();
        Assert.AreEqual((int)BackgroundJobStatus.Processing, entity.Status);
        Assert.AreEqual("worker-active", entity.LockedBy);
        Assert.AreEqual(now.AddMinutes(5), entity.LockedUntil);
    }

    [TestMethod]
    public async Task CompleteAsync_Should_MarkSucceeded()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);

        var id = await queue.EnqueueAsync(BackgroundWorkType.Report);
        _ = await queue.TryClaimNextAsync(BackgroundWorkType.Report, "worker-1", TimeSpan.FromMinutes(5));

        await queue.CompleteAsync(id);

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_BackgroundJobs.FirstAsync(x => x.Id == id);
        Assert.AreEqual(2, entity.Status);
        Assert.IsNotNull(entity.CompletedTime);
    }

    [TestMethod]
    public async Task FailAsync_Should_Reschedule_WhenRetryAvailable()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);

        var id = await queue.EnqueueAsync(BackgroundWorkType.M3u8Refresh, maxRetryCount: 3);
        _ = await queue.TryClaimNextAsync(BackgroundWorkType.M3u8Refresh, "worker-1", TimeSpan.FromMinutes(5));
        var nextRun = DateTime.UtcNow.AddMinutes(1);

        await queue.FailAsync(id, "failed", nextRun);

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_BackgroundJobs.FirstAsync(x => x.Id == id);
        Assert.AreEqual(0, entity.Status);
        Assert.AreEqual(1, entity.RetryCount);
        Assert.AreEqual("failed", entity.LastError);
        Assert.AreEqual(nextRun, entity.ScheduledTime);
    }

    [TestMethod]
    public async Task FailAsync_Should_MarkFailed_WhenRetryCountReachesMaxRetryCount()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);

        var id = await queue.EnqueueAsync(BackgroundWorkType.M3u8Refresh, maxRetryCount: 1);
        _ = await queue.TryClaimNextAsync(BackgroundWorkType.M3u8Refresh, "worker-1", TimeSpan.FromMinutes(5));

        await queue.FailAsync(id, "failed", DateTime.UtcNow.AddMinutes(1));

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_BackgroundJobs.FirstAsync(x => x.Id == id);
        Assert.AreEqual((int)BackgroundJobStatus.Failed, entity.Status);
        Assert.AreEqual(1, entity.RetryCount);
        Assert.IsNotNull(entity.CompletedTime);
    }

    [TestMethod]
    public async Task CountPendingAsync_Should_Filter_ByWorkType()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);

        await queue.EnqueueAsync(BackgroundWorkType.Report);
        await queue.EnqueueAsync(BackgroundWorkType.Report);
        await queue.EnqueueAsync(BackgroundWorkType.AttachmentUpload);

        Assert.AreEqual(3, await queue.CountPendingAsync());
        Assert.AreEqual(2, await queue.CountPendingAsync(BackgroundWorkType.Report));
    }

    [TestMethod]
    public async Task Monitor_GetSummaryAsync_Should_Count_ByStatusAndWorkType()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);
        var monitor = CreateMonitor(scope);

        var pendingReportId = await queue.EnqueueAsync(BackgroundWorkType.Report);
        await queue.EnqueueAsync(BackgroundWorkType.Report);
        await queue.EnqueueAsync(BackgroundWorkType.AttachmentUpload);
        _ = await queue.TryClaimNextAsync(BackgroundWorkType.Report, "worker-1", TimeSpan.FromMinutes(5));
        await queue.CompleteAsync(pendingReportId);

        var summary = await monitor.GetSummaryAsync();

        Assert.AreEqual(2, summary.PendingCount);
        Assert.AreEqual(0, summary.ProcessingCount);
        Assert.AreEqual(1, summary.SucceededCount);
        Assert.AreEqual(2, summary.WorkTypes.First(x => x.WorkType == BackgroundWorkType.Report).PendingCount + summary.WorkTypes.First(x => x.WorkType == BackgroundWorkType.Report).SucceededCount);
        Assert.AreEqual(1, summary.WorkTypes.First(x => x.WorkType == BackgroundWorkType.AttachmentUpload).PendingCount);
    }

    [TestMethod]
    public async Task Monitor_GetListAsync_Should_Filter_ByWorkTypeAndStatus()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);
        var monitor = CreateMonitor(scope);

        await queue.EnqueueAsync(BackgroundWorkType.Report, workKey: "RPT-1");
        await queue.EnqueueAsync(BackgroundWorkType.Report, workKey: "RPT-2");
        await queue.EnqueueAsync(BackgroundWorkType.M3u8Refresh, workKey: "M3U8-1");

        var result = await monitor.GetListAsync(BackgroundWorkType.Report, BackgroundJobStatus.Pending);

        Assert.AreEqual(2, result.TotalCount);
        Assert.AreEqual(2, result.Items.Count);
        Assert.IsTrue(result.Items.All(x => x.WorkType == BackgroundWorkType.Report));
        Assert.IsTrue(result.Items.All(x => x.Status == BackgroundJobStatus.Pending));
    }

    [TestMethod]
    public async Task Monitor_GetListAsync_Should_Page_And_Order_ByCreatedTimeDescending()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var monitor = CreateMonitor(scope);
        var now = DateTime.UtcNow;

        db.Sys_BackgroundJobs.AddRange(
            CreateJob(BackgroundWorkType.Report, BackgroundJobStatus.Pending, now.AddMinutes(-3), "old"),
            CreateJob(BackgroundWorkType.Report, BackgroundJobStatus.Pending, now.AddMinutes(-2), "middle"),
            CreateJob(BackgroundWorkType.Report, BackgroundJobStatus.Pending, now.AddMinutes(-1), "new"));
        await db.SaveChangesAsync();

        var result = await monitor.GetListAsync(page: 2, pageSize: 1);

        Assert.AreEqual(3, result.TotalCount);
        Assert.AreEqual(2, result.Page);
        Assert.AreEqual(1, result.PageSize);
        Assert.AreEqual("middle", result.Items.Single().WorkKey);
    }

    [TestMethod]
    public async Task Monitor_GetListAsync_InvalidPaging_Should_Throw()
    {
        using var scope = BuildScope();
        var monitor = CreateMonitor(scope);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => monitor.GetListAsync(page: 0));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => monitor.GetListAsync(pageSize: 0));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => monitor.GetListAsync(pageSize: 201));
    }

    [TestMethod]
    public async Task Monitor_GetListAsync_InvalidEnum_Should_Throw()
    {
        using var scope = BuildScope();
        var monitor = CreateMonitor(scope);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => monitor.GetListAsync((BackgroundWorkType)999));
        await Assert.ThrowsExceptionAsync<ArgumentException>(() => monitor.GetListAsync(status: (BackgroundJobStatus)999));
    }

    [TestMethod]
    public async Task Monitor_GetByIdAsync_Should_Return_JobDetail()
    {
        using var scope = BuildScope();
        var queue = CreateQueue(scope);
        var monitor = CreateMonitor(scope);

        var id = await queue.EnqueueAsync(BackgroundWorkType.AttachmentUpload, """{"FileId":10}""", workKey: "FILE-10");

        var job = await monitor.GetByIdAsync(id);

        Assert.IsNotNull(job);
        Assert.AreEqual(id, job.Id);
        Assert.AreEqual(BackgroundWorkType.AttachmentUpload, job.WorkType);
        Assert.AreEqual("AttachmentUpload", job.WorkTypeName);
        Assert.AreEqual("FILE-10", job.WorkKey);
        Assert.AreEqual("""{"FileId":10}""", job.PayloadJson);
        Assert.AreEqual("tester", job.CreatedId);
    }

    [TestMethod]
    public async Task Monitor_GetByIdAsync_NotFound_Should_ReturnNull()
    {
        using var scope = BuildScope();
        var monitor = CreateMonitor(scope);

        var job = await monitor.GetByIdAsync(999);

        Assert.IsNull(job);
    }

    [TestMethod]
    public async Task Monitor_GetByIdAsync_InvalidId_Should_Throw()
    {
        using var scope = BuildScope();
        var monitor = CreateMonitor(scope);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => monitor.GetByIdAsync(0));
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"background-job-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        return services.BuildServiceProvider().CreateScope();
    }

    private static DbBackgroundTaskQueue CreateQueue(IServiceScope scope)
    {
        return new DbBackgroundTaskQueue(
            scope.ServiceProvider,
            new BackgroundQueueSettings { DefaultMaxRetryCount = 3 });
    }

    private static BackgroundJobMonitorService CreateMonitor(IServiceScope scope)
    {
        return new BackgroundJobMonitorService(scope.ServiceProvider);
    }

    private static Sys_BackgroundJob CreateJob(
        BackgroundWorkType workType,
        BackgroundJobStatus status,
        DateTime createdTime,
        string workKey)
    {
        return new Sys_BackgroundJob
        {
            WorkType = (int)workType,
            WorkKey = workKey,
            PayloadJson = string.Empty,
            Priority = 0,
            Status = (int)status,
            RetryCount = 0,
            MaxRetryCount = 3,
            ScheduledTime = createdTime,
            CreatedTime = createdTime,
            CreatedId = "seed",
            UpdatedTime = createdTime,
            UpdatedId = "seed",
            Version = Guid.NewGuid()
        };
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new CurrentUser { UserId = "tester" };
    }

    private sealed class RecordingLogService : ILogService
    {
        public List<QueueLogCreateRequest> QueueLogs { get; } = [];

        public Task<long> WriteUserOperationAsync(UserOperationLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(UserOperationLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default)
        {
            QueueLogs.Add(request);
            return Task.FromResult((long)QueueLogs.Count);
        }

        public Task<QueueLogQueryResult> GetQueueLogsAsync(QueueLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<SsoLogQueryResult> GetSsoLogsAsync(SsoLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
