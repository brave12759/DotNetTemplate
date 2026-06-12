using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.BackgroundQueue.Services;
using Template.Common.BackgroundQueue;
using Template.Common.Settings;

namespace Template.Test.Tests;

[TestClass]
public class QueuedBackgroundServiceTests
{
    [TestMethod]
    public async Task StopAsync_Should_CompleteWithoutException()
    {
        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider())),
            new BackgroundQueueSettings { ShutdownTimeoutSeconds = 1 },
            NullLogger<QueuedBackgroundService>.Instance);

        await service.StopAsync(CancellationToken.None);
    }

    [TestMethod]
    public async Task ExecuteAsync_Disabled_Should_ReturnImmediately()
    {
        var scopeFactory = new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider()));
        var service = new QueuedBackgroundService(
            scopeFactory,
            new BackgroundQueueSettings { Enabled = false },
            NullLogger<QueuedBackgroundService>.Instance);

        await InvokeExecuteAsync(service, CancellationToken.None);

        Assert.AreEqual(0, scopeFactory.CreatedCount);
    }

    [TestMethod]
    public async Task ExecuteAsync_NoHandlers_Should_ReturnAfterOneScope()
    {
        var provider = new ServiceCollection().BuildServiceProvider();
        var scopeFactory = new CountingScopeFactory(_ => new SimpleScope(provider));
        var service = new QueuedBackgroundService(
            scopeFactory,
            new BackgroundQueueSettings { Enabled = true },
            NullLogger<QueuedBackgroundService>.Instance);

        await InvokeExecuteAsync(service, CancellationToken.None);

        Assert.AreEqual(1, scopeFactory.CreatedCount);
    }

    [TestMethod]
    public async Task ExecuteJobAsync_Success_Should_CallComplete()
    {
        var queue = new RecordingBackgroundTaskQueue();
        var handler = new FakeBackgroundJobHandler(BackgroundWorkType.Report);
        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider())),
            new BackgroundQueueSettings(),
            NullLogger<QueuedBackgroundService>.Instance);

        await InvokeExecuteJobAsync(service, queue, handler, new BackgroundJob
        {
            Id = 101,
            WorkType = BackgroundWorkType.Report,
            RetryCount = 0
        }, new BackgroundQueueWorkerSettings(), CancellationToken.None);

        Assert.AreEqual(101, queue.CompletedJobId);
        Assert.IsNull(queue.FailedJobId);
    }

    [TestMethod]
    public async Task ExecuteJobAsync_Exception_Should_CallFail()
    {
        var queue = new RecordingBackgroundTaskQueue();
        var handler = new FakeBackgroundJobHandler(BackgroundWorkType.Report)
        {
            HandleAsyncFunc = (_, _) => throw new InvalidOperationException("boom")
        };

        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider())),
            new BackgroundQueueSettings(),
            NullLogger<QueuedBackgroundService>.Instance);

        await InvokeExecuteJobAsync(service, queue, handler, new BackgroundJob
        {
            Id = 202,
            WorkType = BackgroundWorkType.Report,
            RetryCount = 1
        }, new BackgroundQueueWorkerSettings(), CancellationToken.None);

        Assert.AreEqual(202, queue.FailedJobId);
        Assert.IsNotNull(queue.FailedNextRunTime);
        Assert.IsTrue(queue.FailedNextRunTime > DateTime.UtcNow.AddSeconds(30));
    }

    [TestMethod]
    public async Task ExecuteJobAsync_OperationCanceled_WhenStopping_Should_NotCallFail()
    {
        var queue = new RecordingBackgroundTaskQueue();
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new FakeBackgroundJobHandler(BackgroundWorkType.Report)
        {
            HandleAsyncFunc = (_, _) => throw new OperationCanceledException(cts.Token)
        };

        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider())),
            new BackgroundQueueSettings(),
            NullLogger<QueuedBackgroundService>.Instance);

        await InvokeExecuteJobAsync(service, queue, handler, new BackgroundJob
        {
            Id = 303,
            WorkType = BackgroundWorkType.Report,
            RetryCount = 0
        }, new BackgroundQueueWorkerSettings(), cts.Token);

        Assert.IsNull(queue.FailedJobId);
        Assert.IsNull(queue.CompletedJobId);
    }

    [TestMethod]
    public void GetWorkerSettings_ConfiguredWorkType_Should_UseConfiguredValues()
    {
        var settings = new BackgroundQueueSettings
        {
            DefaultPollingIntervalSeconds = 5,
            DefaultLockTimeoutSeconds = 60,
            DefaultMaxRetryCount = 3,
            Workers =
            [
                new BackgroundQueueWorkerSettings
                {
                    WorkType = BackgroundWorkType.SignalRMessage,
                    WorkerCount = 4,
                    PollingIntervalSeconds = 2,
                    LockTimeoutSeconds = 30,
                    MaxRetryCount = 9
                }
            ]
        };

        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider())),
            settings,
            NullLogger<QueuedBackgroundService>.Instance);

        var worker = InvokeGetWorkerSettings(service, BackgroundWorkType.SignalRMessage);

        Assert.AreEqual(4, worker.WorkerCount);
        Assert.AreEqual(2, worker.PollingIntervalSeconds);
        Assert.AreEqual(30, worker.LockTimeoutSeconds);
        Assert.AreEqual(9, worker.MaxRetryCount);
    }

    [TestMethod]
    public void GetWorkerSettings_UnconfiguredWorkType_Should_UseDefaults()
    {
        var settings = new BackgroundQueueSettings
        {
            DefaultPollingIntervalSeconds = 7,
            DefaultLockTimeoutSeconds = 70,
            DefaultMaxRetryCount = 5
        };

        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(new ServiceCollection().BuildServiceProvider())),
            settings,
            NullLogger<QueuedBackgroundService>.Instance);

        var worker = InvokeGetWorkerSettings(service, BackgroundWorkType.Report);

        Assert.AreEqual(BackgroundWorkType.Report, worker.WorkType);
        Assert.AreEqual(1, worker.WorkerCount);
        Assert.AreEqual(7, worker.PollingIntervalSeconds);
        Assert.AreEqual(70, worker.LockTimeoutSeconds);
        Assert.AreEqual(5, worker.MaxRetryCount);
    }

    [TestMethod]
    public async Task RunWorkerAsync_NoMatchingHandler_Should_ThrowOnCancellationAfterDelay()
    {
        var queue = new RecordingBackgroundTaskQueue();
        var services = new ServiceCollection();
        services.AddSingleton<IBackgroundTaskQueue>(queue);
        services.AddSingleton<IEnumerable<IBackgroundJobHandler>>([]);
        var provider = services.BuildServiceProvider();

        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(provider)),
            new BackgroundQueueSettings { DefaultPollingIntervalSeconds = 10, DefaultLockTimeoutSeconds = 30 },
            NullLogger<QueuedBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(30));

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            InvokeRunWorkerAsync(service, BackgroundWorkType.Report, 1, new BackgroundQueueWorkerSettings
            {
                WorkType = BackgroundWorkType.Report,
                PollingIntervalSeconds = 10,
                LockTimeoutSeconds = 30
            }, cts.Token));
    }

    [TestMethod]
    public async Task RunWorkerAsync_ClaimsAndExecutesJob_BeforeCancellation()
    {
        var queue = new RecordingBackgroundTaskQueue
        {
            NextJobFactory = () => new BackgroundJob { Id = 500, WorkType = BackgroundWorkType.Report, RetryCount = 0 }
        };
        var handler = new FakeBackgroundJobHandler(BackgroundWorkType.Report);

        var services = new ServiceCollection();
        services.AddSingleton<IBackgroundTaskQueue>(queue);
        services.AddSingleton<IBackgroundJobHandler>(handler);
        var provider = services.BuildServiceProvider();

        var service = new QueuedBackgroundService(
            new CountingScopeFactory(_ => new SimpleScope(provider)),
            new BackgroundQueueSettings { DefaultPollingIntervalSeconds = 10, DefaultLockTimeoutSeconds = 30 },
            NullLogger<QueuedBackgroundService>.Instance);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsExceptionAsync<TaskCanceledException>(() =>
            InvokeRunWorkerAsync(service, BackgroundWorkType.Report, 1, new BackgroundQueueWorkerSettings
            {
                WorkType = BackgroundWorkType.Report,
                PollingIntervalSeconds = 10,
                LockTimeoutSeconds = 30
            }, cts.Token));

        Assert.AreEqual(500, queue.CompletedJobId);
    }

    private static async Task InvokeExecuteAsync(QueuedBackgroundService service, CancellationToken stoppingToken)
    {
        var method = typeof(QueuedBackgroundService).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new AssertFailedException("ExecuteAsync method not found.");

        var task = method.Invoke(service, [stoppingToken]) as Task
            ?? throw new AssertFailedException("ExecuteAsync invocation did not return Task.");

        await task;
    }

    private static async Task InvokeExecuteJobAsync(
        QueuedBackgroundService service,
        IBackgroundTaskQueue queue,
        IBackgroundJobHandler handler,
        BackgroundJob job,
        BackgroundQueueWorkerSettings workerSettings,
        CancellationToken stoppingToken)
    {
        var method = typeof(QueuedBackgroundService).GetMethod("ExecuteJobAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new AssertFailedException("ExecuteJobAsync method not found.");

        var task = method.Invoke(service, [queue, handler, "worker-ut", job, workerSettings, stoppingToken]) as Task
            ?? throw new AssertFailedException("ExecuteJobAsync invocation did not return Task.");

        await task;
    }

    private static async Task InvokeRunWorkerAsync(
        QueuedBackgroundService service,
        BackgroundWorkType workType,
        int workerNo,
        BackgroundQueueWorkerSettings workerSettings,
        CancellationToken stoppingToken)
    {
        var method = typeof(QueuedBackgroundService).GetMethod("RunWorkerAsync", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new AssertFailedException("RunWorkerAsync method not found.");

        var task = method.Invoke(service, [workType, workerNo, workerSettings, stoppingToken]) as Task
            ?? throw new AssertFailedException("RunWorkerAsync invocation did not return Task.");

        await task;
    }

    private static BackgroundQueueWorkerSettings InvokeGetWorkerSettings(QueuedBackgroundService service, BackgroundWorkType workType)
    {
        var method = typeof(QueuedBackgroundService).GetMethod("GetWorkerSettings", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new AssertFailedException("GetWorkerSettings method not found.");

        var result = method.Invoke(service, [workType]) as BackgroundQueueWorkerSettings;
        return result ?? throw new AssertFailedException("GetWorkerSettings invocation failed.");
    }

    private sealed class CountingScopeFactory(Func<int, IServiceScope> scopeFactory) : IServiceScopeFactory
    {
        private int _createdCount;

        public int CreatedCount => _createdCount;

        public IServiceScope CreateScope()
        {
            _createdCount++;
            return scopeFactory(_createdCount);
        }
    }

    private sealed class SimpleScope(IServiceProvider serviceProvider) : IServiceScope
    {
        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public void Dispose() { }
    }

    private sealed class FakeBackgroundJobHandler(BackgroundWorkType workType) : IBackgroundJobHandler
    {
        public Func<BackgroundJob, CancellationToken, Task>? HandleAsyncFunc { get; set; }

        public BackgroundWorkType WorkType { get; } = workType;

        public Task HandleAsync(BackgroundJob job, CancellationToken cancellationToken)
            => HandleAsyncFunc?.Invoke(job, cancellationToken) ?? Task.CompletedTask;
    }

    private sealed class RecordingBackgroundTaskQueue : IBackgroundTaskQueue
    {
        private int _claimCalls;

        public Func<BackgroundJob?>? NextJobFactory { get; set; }
        public long? CompletedJobId { get; private set; }
        public long? FailedJobId { get; private set; }
        public DateTime? FailedNextRunTime { get; private set; }

        public Task<long> EnqueueAsync(BackgroundWorkType workType, string payloadJson = "", string? workKey = null, int priority = 0, DateTime? scheduledTime = null, int? maxRetryCount = null, CancellationToken cancellationToken = default)
            => Task.FromResult(1L);

        public Task<BackgroundJob?> TryClaimNextAsync(BackgroundWorkType workType, string workerId, TimeSpan lockTimeout, CancellationToken cancellationToken = default)
        {
            _claimCalls++;
            if (_claimCalls == 1)
                return Task.FromResult(NextJobFactory?.Invoke());

            return Task.FromResult<BackgroundJob?>(null);
        }

        public Task CompleteAsync(long jobId, CancellationToken cancellationToken = default)
        {
            CompletedJobId = jobId;
            return Task.CompletedTask;
        }

        public Task FailAsync(long jobId, string errorMessage, DateTime? nextRunTime, CancellationToken cancellationToken = default)
        {
            FailedJobId = jobId;
            FailedNextRunTime = nextRunTime;
            return Task.CompletedTask;
        }

        public Task<int> CountPendingAsync(BackgroundWorkType? workType = null, CancellationToken cancellationToken = default)
            => Task.FromResult(0);
    }
}
