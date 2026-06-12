using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.SignalR.Services;
using Template.Common.BackgroundQueue;
using Template.Common.SignalR;
using Template.WebApi.Hubs;
using Template.WebApi.SignalR;

namespace Template.Test.Tests;

[TestClass]
public class SignalRQueueServiceTests
{
    [TestMethod]
    public async Task QueueGroupAsync_Should_Enqueue_SignalRMessageJob()
    {
        var queue = new FakeBackgroundTaskQueue();
        var service = new SignalRQueueService(queue);

        var jobId = await service.QueueGroupAsync(
            "admins",
            SignalRClientMethods.Notification,
            new { Message = "hello" },
            priority: 10);

        Assert.AreEqual(100, jobId);
        Assert.AreEqual(BackgroundWorkType.SignalRMessage, queue.WorkType);
        Assert.AreEqual("Group:admins:Notification", queue.WorkKey);
        Assert.AreEqual(10, queue.Priority);

        var message = JsonSerializer.Deserialize<SignalRQueuedMessage>(queue.PayloadJson);
        Assert.IsNotNull(message);
        Assert.AreEqual(SignalRTargetType.Group, message.TargetType);
        Assert.AreEqual("admins", message.Target);
        Assert.AreEqual(SignalRClientMethods.Notification, message.Method);
        Assert.IsNotNull(message.Payload);
    }

    [TestMethod]
    public async Task QueueAllAsync_Should_NotRequire_Target()
    {
        var queue = new FakeBackgroundTaskQueue();
        var service = new SignalRQueueService(queue);

        await service.QueueAllAsync(SignalRClientMethods.BackgroundJobChanged, new { JobId = 1 });

        Assert.AreEqual("All:BackgroundJobChanged", queue.WorkKey);

        var message = JsonSerializer.Deserialize<SignalRQueuedMessage>(queue.PayloadJson);
        Assert.IsNotNull(message);
        Assert.AreEqual(SignalRTargetType.All, message.TargetType);
        Assert.AreEqual(string.Empty, message.Target);
    }

    [TestMethod]
    public async Task QueueUserAsync_EmptyUserId_Should_Throw()
    {
        var queue = new FakeBackgroundTaskQueue();
        var service = new SignalRQueueService(queue);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            service.QueueUserAsync("", SignalRClientMethods.Notification, null));
    }

    [TestMethod]
    public async Task Handler_Should_Send_To_AllClients()
    {
        var clients = new FakeHubClients();
        var handler = CreateHandler(clients);

        await handler.HandleAsync(CreateJob(new SignalRQueuedMessage
        {
            TargetType = SignalRTargetType.All,
            Method = SignalRClientMethods.Notification,
            Payload = new { Message = "hello" }
        }), CancellationToken.None);

        Assert.AreEqual("All", clients.LastClientProxyKey);
        Assert.AreEqual(SignalRClientMethods.Notification, clients.LastSend.Method);
        Assert.AreEqual(1, clients.LastSend.Args.Length);
        Assert.IsInstanceOfType<JsonElement>(clients.LastSend.Args[0]);
    }

    [TestMethod]
    public async Task Handler_Should_Send_To_Group()
    {
        var clients = new FakeHubClients();
        var handler = CreateHandler(clients);

        await handler.HandleAsync(CreateJob(new SignalRQueuedMessage
        {
            TargetType = SignalRTargetType.Group,
            Target = "admins",
            Method = SignalRClientMethods.BackgroundJobChanged,
            Payload = new { JobId = 1 }
        }), CancellationToken.None);

        Assert.AreEqual("Group:admins", clients.LastClientProxyKey);
        Assert.AreEqual(SignalRClientMethods.BackgroundJobChanged, clients.LastSend.Method);
    }

    [TestMethod]
    public async Task Handler_Should_Send_To_User()
    {
        var clients = new FakeHubClients();
        var handler = CreateHandler(clients);

        await handler.HandleAsync(CreateJob(new SignalRQueuedMessage
        {
            TargetType = SignalRTargetType.User,
            Target = "user-1",
            Method = SignalRClientMethods.Notification,
            Payload = null
        }), CancellationToken.None);

        Assert.AreEqual("User:user-1", clients.LastClientProxyKey);
        Assert.AreEqual(SignalRClientMethods.Notification, clients.LastSend.Method);
    }

    [TestMethod]
    public async Task Handler_Should_Send_To_Connection()
    {
        var clients = new FakeHubClients();
        var handler = CreateHandler(clients);

        await handler.HandleAsync(CreateJob(new SignalRQueuedMessage
        {
            TargetType = SignalRTargetType.Connection,
            Target = "conn-1",
            Method = SignalRClientMethods.Notification,
            Payload = new { Value = 1 }
        }), CancellationToken.None);

        Assert.AreEqual("Client:conn-1", clients.LastClientProxyKey);
        Assert.AreEqual(SignalRClientMethods.Notification, clients.LastSend.Method);
    }

    [TestMethod]
    public async Task Handler_InvalidPayload_Should_Throw()
    {
        var handler = CreateHandler(new FakeHubClients());
        var job = new BackgroundJob
        {
            Id = 1,
            WorkType = BackgroundWorkType.SignalRMessage,
            PayloadJson = "{}"
        };

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
            handler.HandleAsync(job, CancellationToken.None));
    }

    [TestMethod]
    public async Task NotificationHub_JoinGroup_Should_Trim_And_AddConnection()
    {
        var groups = new FakeGroupManager();
        var hub = new NotificationHub
        {
            Context = new FakeHubCallerContext("conn-1"),
            Groups = groups
        };

        await hub.JoinGroup(" admins ");

        Assert.AreEqual("conn-1", groups.LastConnectionId);
        Assert.AreEqual("admins", groups.LastGroupName);
        Assert.AreEqual("Add", groups.LastAction);
    }

    [TestMethod]
    public async Task NotificationHub_LeaveGroup_Should_Trim_And_RemoveConnection()
    {
        var groups = new FakeGroupManager();
        var hub = new NotificationHub
        {
            Context = new FakeHubCallerContext("conn-1"),
            Groups = groups
        };

        await hub.LeaveGroup(" admins ");

        Assert.AreEqual("conn-1", groups.LastConnectionId);
        Assert.AreEqual("admins", groups.LastGroupName);
        Assert.AreEqual("Remove", groups.LastAction);
    }

    [TestMethod]
    public async Task NotificationHub_EmptyGroup_Should_Throw()
    {
        var hub = new NotificationHub
        {
            Context = new FakeHubCallerContext("conn-1"),
            Groups = new FakeGroupManager()
        };

        await Assert.ThrowsExceptionAsync<HubException>(() => hub.JoinGroup(""));
    }

    private static QueuedSignalRMessageHandler CreateHandler(FakeHubClients clients)
    {
        return new QueuedSignalRMessageHandler(
            new FakeHubContext(clients),
            NullLogger<QueuedSignalRMessageHandler>.Instance);
    }

    private static BackgroundJob CreateJob(SignalRQueuedMessage message)
    {
        return new BackgroundJob
        {
            Id = 1,
            WorkType = BackgroundWorkType.SignalRMessage,
            PayloadJson = JsonSerializer.Serialize(message)
        };
    }

    private sealed class FakeBackgroundTaskQueue : IBackgroundTaskQueue
    {
        public BackgroundWorkType WorkType { get; private set; }

        public string PayloadJson { get; private set; } = string.Empty;

        public string? WorkKey { get; private set; }

        public int Priority { get; private set; }

        public Task<long> EnqueueAsync(
            BackgroundWorkType workType,
            string payloadJson = "",
            string? workKey = null,
            int priority = 0,
            DateTime? scheduledTime = null,
            int? maxRetryCount = null,
            CancellationToken cancellationToken = default)
        {
            WorkType = workType;
            PayloadJson = payloadJson;
            WorkKey = workKey;
            Priority = priority;
            return Task.FromResult(100L);
        }

        public Task<BackgroundJob?> TryClaimNextAsync(
            BackgroundWorkType workType,
            string workerId,
            TimeSpan lockTimeout,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task CompleteAsync(long jobId, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task FailAsync(
            long jobId,
            string errorMessage,
            DateTime? nextRunTime,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public Task<int> CountPendingAsync(
            BackgroundWorkType? workType = null,
            CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }
    }

    private sealed class FakeHubContext(FakeHubClients clients) : IHubContext<NotificationHub>
    {
        public IHubClients Clients { get; } = clients;

        public IGroupManager Groups { get; } = new FakeGroupManager();
    }

    private sealed class FakeHubClients : IHubClients
    {
        private readonly FakeClientProxy _proxy = new();

        public string LastClientProxyKey { get; private set; } = string.Empty;

        public SentSignalRMessage LastSend => _proxy.LastSend;

        public IClientProxy All
        {
            get
            {
                LastClientProxyKey = "All";
                return _proxy;
            }
        }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds)
        {
            LastClientProxyKey = $"AllExcept:{string.Join(",", excludedConnectionIds)}";
            return _proxy;
        }

        public IClientProxy Client(string connectionId)
        {
            LastClientProxyKey = $"Client:{connectionId}";
            return _proxy;
        }

        public IClientProxy Clients(IReadOnlyList<string> connectionIds)
        {
            LastClientProxyKey = $"Clients:{string.Join(",", connectionIds)}";
            return _proxy;
        }

        public IClientProxy Group(string groupName)
        {
            LastClientProxyKey = $"Group:{groupName}";
            return _proxy;
        }

        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds)
        {
            LastClientProxyKey = $"GroupExcept:{groupName}:{string.Join(",", excludedConnectionIds)}";
            return _proxy;
        }

        public IClientProxy Groups(IReadOnlyList<string> groupNames)
        {
            LastClientProxyKey = $"Groups:{string.Join(",", groupNames)}";
            return _proxy;
        }

        public IClientProxy User(string userId)
        {
            LastClientProxyKey = $"User:{userId}";
            return _proxy;
        }

        public IClientProxy Users(IReadOnlyList<string> userIds)
        {
            LastClientProxyKey = $"Users:{string.Join(",", userIds)}";
            return _proxy;
        }
    }

    private sealed class FakeClientProxy : IClientProxy
    {
        public SentSignalRMessage LastSend { get; private set; } = new(string.Empty, [], CancellationToken.None);

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            LastSend = new SentSignalRMessage(method, args, cancellationToken);
            return Task.CompletedTask;
        }
    }

    private sealed record SentSignalRMessage(
        string Method,
        object?[] Args,
        CancellationToken CancellationToken);

    private sealed class FakeGroupManager : IGroupManager
    {
        public string LastConnectionId { get; private set; } = string.Empty;

        public string LastGroupName { get; private set; } = string.Empty;

        public string LastAction { get; private set; } = string.Empty;

        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            LastConnectionId = connectionId;
            LastGroupName = groupName;
            LastAction = "Add";
            return Task.CompletedTask;
        }

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
        {
            LastConnectionId = connectionId;
            LastGroupName = groupName;
            LastAction = "Remove";
            return Task.CompletedTask;
        }
    }

    private sealed class FakeHubCallerContext(string connectionId) : HubCallerContext
    {
        public override string ConnectionId { get; } = connectionId;

        public override string? UserIdentifier { get; } = "user-1";

        public override ClaimsPrincipal? User { get; } = new(new ClaimsIdentity());

        public override IDictionary<object, object?> Items { get; } = new Dictionary<object, object?>();

        public override IFeatureCollection Features { get; } = new FeatureCollection();

        public override CancellationToken ConnectionAborted { get; } = CancellationToken.None;

        public override void Abort()
        {
        }
    }
}
