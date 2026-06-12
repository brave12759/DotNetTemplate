using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.DepartmentService.Models;
using Template.BusinessRule.DepartmentService.Services;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class DepartmentServiceTests
{
    [TestMethod]
    public async Task CreateAsync_Should_CreateDepartment_AndSetAuditFields()
    {
        using var scope = BuildScope();
        var sut = new DepartmentService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new DepartmentCreateRequest
        {
            DeptName = "  IT  ",
            SortOrder = 10,
            IsEnable = true
        });

        Assert.IsTrue(created.DeptId > 0);
        Assert.AreEqual("IT", created.DeptName);
        Assert.AreEqual("tester", created.CreatedId);
        Assert.AreEqual("tester", created.UpdatedId);

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_Departments.FirstAsync(x => x.DeptId == created.DeptId);
        Assert.AreEqual("tester", entity.CreatedId);
        Assert.AreEqual("tester", entity.UpdatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual("Department", logService.UserOperations[0].Module);
        Assert.AreEqual(AuditActionEnum.Create, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task CreateAsync_ParentDoesNotExist_Should_Throw()
    {
        using var scope = BuildScope();
        var sut = new DepartmentService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.CreateAsync(new DepartmentCreateRequest
            {
                DeptName = "IT",
                ParentDeptId = 999,
                SortOrder = 10,
                IsEnable = true
            }));
    }

    [TestMethod]
    public async Task GetTreeAsync_Should_ReturnParentChildHierarchy()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateEntity("Head Office", null, 20);
        var otherRoot = CreateEntity("Sales", null, 10);
        db.Sys_Departments.AddRange(root, otherRoot);
        await db.SaveChangesAsync();

        db.Sys_Departments.AddRange(
            CreateEntity("Infrastructure", root.DeptId, 20),
            CreateEntity("Applications", root.DeptId, 10));
        await db.SaveChangesAsync();

        var sut = new DepartmentService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(2, tree.Count);
        Assert.AreEqual("Sales", tree[0].DeptName);
        Assert.AreEqual("Head Office", tree[1].DeptName);
        Assert.AreEqual(2, tree[1].Children.Count);
        Assert.AreEqual("Applications", tree[1].Children[0].DeptName);
        Assert.AreEqual("Infrastructure", tree[1].Children[1].DeptName);
    }

    [TestMethod]
    public async Task UpdateAsync_Should_UpdateDepartment_AndSetUpdatedId()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = CreateEntity("IT");
        db.Sys_Departments.Add(department);
        await db.SaveChangesAsync();

        var sut = new DepartmentService(scope.ServiceProvider);
        var ok = await sut.UpdateAsync(new DepartmentUpdateRequest
        {
            DeptId = department.DeptId,
            DeptName = "Information Technology",
            SortOrder = 30,
            IsEnable = false
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_Departments.FirstAsync(x => x.DeptId == department.DeptId);
        Assert.AreEqual("Information Technology", updated.DeptName);
        Assert.AreEqual(30, updated.SortOrder);
        Assert.IsFalse(updated.IsEnable);
        Assert.AreEqual("tester", updated.UpdatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual(AuditActionEnum.Update, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task UpdateAsync_ParentIsDescendant_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateEntity("Head Office");
        db.Sys_Departments.Add(root);
        await db.SaveChangesAsync();

        var child = CreateEntity("IT", root.DeptId);
        db.Sys_Departments.Add(child);
        await db.SaveChangesAsync();

        var sut = new DepartmentService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.UpdateAsync(new DepartmentUpdateRequest
            {
                DeptId = root.DeptId,
                ParentDeptId = child.DeptId,
                DeptName = "Head Office",
                IsEnable = true
            }));
    }

    [TestMethod]
    public async Task DeleteAsync_DepartmentHasChildren_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateEntity("Head Office");
        db.Sys_Departments.Add(root);
        await db.SaveChangesAsync();

        db.Sys_Departments.Add(CreateEntity("IT", root.DeptId));
        await db.SaveChangesAsync();

        var sut = new DepartmentService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.DeleteAsync(root.DeptId));
    }

    [TestMethod]
    public async Task DeleteAsync_DepartmentHasUsers_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var department = CreateEntity("IT");
        db.Sys_Departments.Add(department);
        await db.SaveChangesAsync();

        db.Sys_UserInfos.Add(new Sys_UserInfo
        {
            UserId = "it-user",
            UserName = "IT User",
            Password = "HASH::Password@123",
            DeptId = department.DeptId,
            MobilePhone = "0911111111",
            Email = "it-user@example.com",
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new DepartmentService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.DeleteAsync(department.DeptId));
    }

    [TestMethod]
    public async Task DeleteAsync_LeafDepartment_Should_Remove()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var department = CreateEntity("IT");
        db.Sys_Departments.Add(department);
        await db.SaveChangesAsync();

        var sut = new DepartmentService(scope.ServiceProvider);
        var ok = await sut.DeleteAsync(department.DeptId);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_Departments.AnyAsync(x => x.DeptId == department.DeptId));

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual(AuditActionEnum.Delete, logService.UserOperations[0].Action);
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"department-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sys_Department CreateEntity(
        string deptName,
        int? parentDeptId = null,
        int sortOrder = 10)
    {
        return new Sys_Department
        {
            DeptName = deptName,
            ParentDeptId = parentDeptId,
            SortOrder = sortOrder,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new CurrentUser { UserId = "tester", Email = "tester@localhost" };
    }

    private sealed class RecordingLogService : ILogService
    {
        public List<UserOperationLogCreateRequest> UserOperations { get; } = [];

        public Task<long> WriteUserOperationAsync(UserOperationLogCreateRequest request, CancellationToken cancellationToken = default)
        {
            UserOperations.Add(request);
            return Task.FromResult((long)UserOperations.Count);
        }

        public Task<UserOperationLogQueryResult> GetUserOperationLogsAsync(UserOperationLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteQueueAsync(QueueLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<QueueLogQueryResult> GetQueueLogsAsync(QueueLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();

        public Task<long> WriteSsoAsync(SsoLogCreateRequest request, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<SsoLogQueryResult> GetSsoLogsAsync(SsoLogQueryRequest request, CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
