using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.LogDbContext;

namespace Template.Test.Tests;

[TestClass]
public class LogServiceTests
{
    [TestMethod]
    public async Task WriteUserOperationAsync_MissingModule_Should_Throw()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => sut.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = " ",
            Action = AuditActionEnum.Create,
            Result = AuditResultEnum.Success
        }));
    }

    [TestMethod]
    public async Task WriteUserOperationAsync_InvalidActionOrResult_Should_Throw()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => sut.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "User",
            Action = (AuditActionEnum)999,
            Result = AuditResultEnum.Success
        }));

        await Assert.ThrowsExceptionAsync<ArgumentException>(() => sut.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "User",
            Action = AuditActionEnum.Create,
            Result = (AuditResultEnum)999
        }));
    }

    [TestMethod]
    public async Task WriteUserOperationAsync_Should_UseFactoryWriter_AndPersistUserOperationLog()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        var id = await sut.WriteUserOperationAsync(new UserOperationLogCreateRequest
        {
            Module = "User",
            Action = AuditActionEnum.PasswordReset,
            Result = AuditResultEnum.Success,
            TargetType = "Sys_UserInfo",
            TargetId = "alice",
            Message = "重設使用者密碼。",
            Metadata = new { PasswordChangeType = UserPasswordChangeTypeEnum.Reset.ToString() }
        });

        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var entity = await db.UserOperationLogs.SingleAsync(x => x.Id == id);

        Assert.AreEqual("admin", entity.UserId);
        Assert.AreEqual("User", entity.Module);
        Assert.AreEqual((int)AuditActionEnum.PasswordReset, entity.Action);
        Assert.AreEqual((int)AuditResultEnum.Success, entity.Result);
        Assert.AreEqual("alice", entity.TargetId);
        Assert.IsTrue(entity.MetadataJson.Contains("Reset", StringComparison.Ordinal));
    }

    [TestMethod]
    public async Task GetUserOperationLogsAsync_Should_Filter_AndReturnPagedResult()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        db.UserOperationLogs.AddRange(
            CreateUserOperation("User", AuditActionEnum.Create, "alice", DateTime.UtcNow.AddMinutes(-2)),
            CreateUserOperation("Department", AuditActionEnum.Update, "10", DateTime.UtcNow.AddMinutes(-1)));
        await db.SaveChangesAsync();

        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();
        var result = await sut.GetUserOperationLogsAsync(new UserOperationLogQueryRequest
        {
            Module = "User",
            Page = 1,
            PageSize = 10
        });

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("alice", result.Items[0].TargetId);
        Assert.AreEqual(AuditActionEnum.Create, result.Items[0].Action);
    }

    [TestMethod]
    public async Task WriteQueueAsync_Should_PersistQueueLog()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        var id = await sut.WriteQueueAsync(new QueueLogCreateRequest
        {
            OperatorId = "worker-1",
            JobId = 10,
            WorkType = 1,
            WorkKey = "report-10",
            EventName = "Complete",
            Status = 2,
            RetryCount = 1,
            Message = "背景工作執行成功。"
        });

        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var entity = await db.QueueLogs.SingleAsync(x => x.Id == id);

        Assert.AreEqual("worker-1", entity.OperatorId);
        Assert.AreEqual(10, entity.JobId);
        Assert.AreEqual("Complete", entity.EventName);
    }

    [TestMethod]
    public async Task WriteSsoAsync_Should_PersistSsoLog()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        var id = await sut.WriteSsoAsync(new SsoLogCreateRequest
        {
            OperatorId = "client-a",
            ClientId = "client-a",
            EventName = "Login",
            Result = "Success",
            IpAddress = "127.0.0.1",
            Message = "SSO 登入成功。"
        });

        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var entity = await db.SsoLogs.SingleAsync(x => x.Id == id);

        Assert.AreEqual("client-a", entity.OperatorId);
        Assert.AreEqual("client-a", entity.ClientId);
        Assert.AreEqual("Success", entity.Result);
    }

    [TestMethod]
    public async Task WriteQueueAsync_EmptyOperator_Should_FallbackCurrentUser()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        var id = await sut.WriteQueueAsync(new QueueLogCreateRequest
        {
            OperatorId = " ",
            JobId = 99,
            WorkType = 1,
            WorkKey = "k",
            EventName = "E",
            Status = 1,
            RetryCount = 0,
            Message = "m"
        });

        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        var entity = await db.QueueLogs.SingleAsync(x => x.Id == id);
        Assert.AreEqual("admin", entity.OperatorId);
    }

    [TestMethod]
    public async Task GetQueueLogsAsync_WithFilters_Should_ReturnFiltered()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        db.QueueLogs.AddRange(
            new QueueLog { EventTime = DateTime.UtcNow.AddMinutes(-2), OperatorId = "worker-1", JobId = 1, WorkType = 1, WorkKey = "a", EventName = "E", Status = 1, RetryCount = 0, Message = "m" },
            new QueueLog { EventTime = DateTime.UtcNow.AddMinutes(-1), OperatorId = "worker-2", JobId = 2, WorkType = 2, WorkKey = "b", EventName = "E", Status = 1, RetryCount = 0, Message = "m" });
        await db.SaveChangesAsync();

        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();
        var result = await sut.GetQueueLogsAsync(new QueueLogQueryRequest { OperatorId = "worker-1", Page = 1, PageSize = 10 });

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("worker-1", result.Items.Single().OperatorId);
    }

    [TestMethod]
    public async Task GetSsoLogsAsync_WithFilters_Should_ReturnFiltered()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<LogDbContext>();
        db.SsoLogs.AddRange(
            new SsoLog { EventTime = DateTime.UtcNow.AddMinutes(-2), OperatorId = "client-a", ClientId = "client-a", EventName = "Login", Result = "Success", IpAddress = "127.0.0.1", Message = "ok" },
            new SsoLog { EventTime = DateTime.UtcNow.AddMinutes(-1), OperatorId = "client-b", ClientId = "client-b", EventName = "Login", Result = "Success", IpAddress = "127.0.0.1", Message = "ok" });
        await db.SaveChangesAsync();

        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();
        var result = await sut.GetSsoLogsAsync(new SsoLogQueryRequest { OperatorId = "client-a", Page = 1, PageSize = 10 });

        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual("client-a", result.Items.Single().OperatorId);
    }

    [TestMethod]
    public async Task GetQueueLogsAsync_InvalidPaging_Should_Throw()
    {
        using var scope = BuildScope();
        var sut = scope.ServiceProvider.GetRequiredService<ILogService>();

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.GetQueueLogsAsync(new QueueLogQueryRequest { Page = 0, PageSize = 10 }));
    }

    [TestMethod]
    public void LogWriterFactory_UnknownType_Should_Throw()
    {
        var factory = new LogWriterFactory([]);

        Assert.ThrowsException<NotSupportedException>(() =>
            factory.Create(LogWriterTypeEnum.UserOperation));
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<LogDbContext>(options =>
            options.UseInMemoryDatabase($"log-service-tests-{Guid.NewGuid()}"));
        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddScoped<ILogWriter, UserOperationLogWriter>();
        services.AddScoped<ILogWriter, QueueLogWriter>();
        services.AddScoped<ILogWriter, SsoLogWriter>();
        services.AddScoped<ILogWriterFactory, LogWriterFactory>();
        services.AddScoped<ILogService, LogService>();

        return services.BuildServiceProvider().CreateScope();
    }

    private static UserOperationLog CreateUserOperation(
        string module,
        AuditActionEnum action,
        string targetId,
        DateTime eventTime)
    {
        return new UserOperationLog
        {
            EventTime = eventTime,
            UserId = "admin",
            Module = module,
            Action = (int)action,
            Result = (int)AuditResultEnum.Success,
            TargetType = "Test",
            TargetId = targetId,
            Message = "測試資料"
        };
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new CurrentUser { UserId = "admin" };
    }
}
