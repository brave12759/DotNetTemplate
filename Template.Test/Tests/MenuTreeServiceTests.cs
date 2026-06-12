using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.MenuTreeService.Models;
using Template.BusinessRule.MenuTreeService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class MenuTreeServiceTests
{
    [TestMethod]
    public async Task CreateAsync_Should_CreateMenu_AndSetAuditFields()
    {
        using var scope = BuildScope();
        var sut = new MenuTreeService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new MenuTreeCreateRequest
        {
            MenuCode = "  SYSTEM  ",
            MenuName = "系統管理",
            Icon = "settings",
            SortOrder = 10,
            IsEnable = true
        });

        Assert.IsTrue(created.Id > 0);
        Assert.AreEqual("SYSTEM", created.MenuCode);
        Assert.AreEqual("系統管理", created.MenuName);
        Assert.AreEqual("tester", created.CreatedId);
        Assert.AreEqual("tester", created.UpdatedId);

        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var entity = await db.Sys_MenuTrees.FirstAsync(x => x.Id == created.Id);
        Assert.AreEqual("tester", entity.CreatedId);
        Assert.AreEqual("tester", entity.UpdatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual("MenuTree", logService.UserOperations[0].Module);
        Assert.AreEqual(AuditActionEnum.Create, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task GetTreeAsync_Should_ReturnParentChildHierarchy()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateEntity("SYSTEM", "系統管理", null, 20);
        var otherRoot = CreateEntity("DASHBOARD", "儀表板", null, 10);
        db.Sys_MenuTrees.AddRange(root, otherRoot);
        await db.SaveChangesAsync();

        db.Sys_MenuTrees.AddRange(
            CreateEntity("SYSTEM_USER", "使用者管理", root.Id, 20),
            CreateEntity("SYSTEM_MENU", "選單管理", root.Id, 10));
        await db.SaveChangesAsync();

        var sut = new MenuTreeService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(2, tree.Count);
        Assert.AreEqual("DASHBOARD", tree[0].MenuCode);
        Assert.AreEqual("SYSTEM", tree[1].MenuCode);
        Assert.AreEqual(2, tree[1].Children.Count);
        Assert.AreEqual("SYSTEM_MENU", tree[1].Children[0].MenuCode);
        Assert.AreEqual("SYSTEM_USER", tree[1].Children[1].MenuCode);
    }

    [TestMethod]
    public async Task CreateAsync_DuplicateMenuCode_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sys_MenuTrees.Add(CreateEntity("SYSTEM", "系統管理"));
        await db.SaveChangesAsync();

        var sut = new MenuTreeService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.CreateAsync(new MenuTreeCreateRequest
            {
                MenuCode = "SYSTEM",
                MenuName = "重複選單",
                Icon = "copy"
            }));
    }

    [TestMethod]
    public async Task UpdateAsync_Should_UpdateMenu_AndSetUpdatedId()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var menu = CreateEntity("SYSTEM", "系統管理");
        db.Sys_MenuTrees.Add(menu);
        await db.SaveChangesAsync();

        var sut = new MenuTreeService(scope.ServiceProvider);
        var ok = await sut.UpdateAsync(new MenuTreeUpdateRequest
        {
            Id = menu.Id,
            MenuCode = "SYSTEM_ADMIN",
            MenuName = "系統後台",
            Icon = "shield",
            SortOrder = 30,
            IsEnable = false
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_MenuTrees.FirstAsync(x => x.Id == menu.Id);
        Assert.AreEqual("SYSTEM_ADMIN", updated.MenuCode);
        Assert.AreEqual("系統後台", updated.MenuName);
        Assert.AreEqual("shield", updated.Icon);
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

        var root = CreateEntity("SYSTEM", "系統管理");
        db.Sys_MenuTrees.Add(root);
        await db.SaveChangesAsync();

        var child = CreateEntity("SYSTEM_MENU", "選單管理", root.Id);
        db.Sys_MenuTrees.Add(child);
        await db.SaveChangesAsync();

        var sut = new MenuTreeService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.UpdateAsync(new MenuTreeUpdateRequest
            {
                Id = root.Id,
                ParentId = child.Id,
                MenuCode = "SYSTEM",
                MenuName = "系統管理",
                IsEnable = true
            }));
    }

    [TestMethod]
    public async Task DeleteAsync_MenuHasChildren_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateEntity("SYSTEM", "系統管理");
        db.Sys_MenuTrees.Add(root);
        await db.SaveChangesAsync();

        db.Sys_MenuTrees.Add(CreateEntity("SYSTEM_MENU", "選單管理", root.Id));
        await db.SaveChangesAsync();

        var sut = new MenuTreeService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => sut.DeleteAsync(root.Id));
    }

    [TestMethod]
    public async Task DeleteAsync_LeafMenu_Should_Remove()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        var menu = CreateEntity("SYSTEM", "系統管理");
        db.Sys_MenuTrees.Add(menu);
        await db.SaveChangesAsync();

        var sut = new MenuTreeService(scope.ServiceProvider);
        var ok = await sut.DeleteAsync(menu.Id);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_MenuTrees.AnyAsync(x => x.Id == menu.Id));

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual(AuditActionEnum.Delete, logService.UserOperations[0].Action);
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"menu-tree-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sys_MenuTree CreateEntity(
        string menuCode,
        string menuName,
        int? parentId = null,
        int sortOrder = 10)
    {
        return new Sys_MenuTree
        {
            ParentId = parentId,
            MenuCode = menuCode,
            MenuName = menuName,
            Icon = string.Empty,
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
