using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"background-job-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

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

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new CurrentUser { UserId = "tester" };
    }
}
