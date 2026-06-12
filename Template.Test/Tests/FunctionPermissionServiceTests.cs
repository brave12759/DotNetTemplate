using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.LogService.Models;
using Template.BusinessRule.LogService.Services;
using Template.BusinessRule.FunctionPermissionService.Models;
using Template.BusinessRule.FunctionPermissionService.Services;
using Template.Common.Enums;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class FunctionPermissionServiceTests
{
    [TestMethod]
    public async Task CreateAsync_Should_CreatePermission_AndSetAuditFields()
    {
        using var scope = BuildScope();
        var sut = new FunctionPermissionService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new FunctionPermissionCreateRequest
        {
            FunctionCode = "USER",
            FunctionName = "使用者管理",
            OperationCode = "C",
            SortOrder = 10,
            IsEnable = true
        });

        Assert.IsTrue(created.FunctionPermissionId > 0);
        Assert.AreEqual("USER:C", created.PermissionKey);
        Assert.AreEqual("新增", created.OperationName);
        Assert.AreEqual("tester", created.CreatedId);
        Assert.AreEqual("tester", created.UpdatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual("FunctionPermission", logService.UserOperations[0].Module);
        Assert.AreEqual(AuditActionEnum.Create, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task CreateAsync_DuplicatePermissionKey_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sys_FunctionPermissions.Add(CreateFunctionPermission("USER", "User", "R"));
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.CreateAsync(new FunctionPermissionCreateRequest
            {
                FunctionCode = "USER",
                FunctionName = "User",
                OperationCode = "R",
                SortOrder = 10,
                IsEnable = true
            }));
    }

    [TestMethod]
    public async Task GetTreeAsync_Should_ReturnParentChildHierarchy()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateFunctionPermission("USER", "使用者管理", null, null, 20);
        var otherRoot = CreateFunctionPermission("ROLE", "角色群組", null, null, 10);
        db.Sys_FunctionPermissions.AddRange(root, otherRoot);
        await db.SaveChangesAsync();

        db.Sys_FunctionPermissions.AddRange(
            CreateFunctionPermission("USER", "使用者管理", "U", root.FunctionPermissionId, 20),
            CreateFunctionPermission("USER", "使用者管理", "R", root.FunctionPermissionId, 10));
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(2, tree.Count);
        Assert.AreEqual("ROLE", tree[0].PermissionKey);
        Assert.AreEqual("USER", tree[1].PermissionKey);
        Assert.AreEqual(2, tree[1].Children.Count);
        Assert.AreEqual("USER:R", tree[1].Children[0].PermissionKey);
        Assert.AreEqual("USER:U", tree[1].Children[1].PermissionKey);
    }

    [TestMethod]
    public async Task UpdateAsync_Should_UpdatePermission_ByFunctionPermissionId()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var permission = CreateFunctionPermission("USER", "使用者管理", "R");
        db.Sys_FunctionPermissions.Add(permission);
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var ok = await sut.UpdateAsync(new FunctionPermissionUpdateRequest
        {
            FunctionPermissionId = permission.FunctionPermissionId,
            FunctionCode = "USER",
            FunctionName = "使用者維護",
            OperationCode = "U",
            SortOrder = 30,
            IsEnable = false
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_FunctionPermissions.FirstAsync(x => x.FunctionPermissionId == permission.FunctionPermissionId);
        Assert.AreEqual("USER:U", updated.PermissionKey);
        Assert.AreEqual("使用者維護", updated.FunctionName);
        Assert.AreEqual("更新", updated.OperationName);
        Assert.AreEqual(30, updated.SortOrder);
        Assert.IsFalse(updated.IsEnable);
        Assert.AreEqual("tester", updated.UpdatedId);

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual(AuditActionEnum.Update, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task UpdateAsync_DuplicatePermissionKey_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        db.Sys_FunctionPermissions.AddRange(
            CreateFunctionPermission("USER", "User", "R"),
            CreateFunctionPermission("ROLE", "Role", "R"));
        await db.SaveChangesAsync();

        var rolePermission = await db.Sys_FunctionPermissions.FirstAsync(x => x.FunctionCode == "ROLE");
        var sut = new FunctionPermissionService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.UpdateAsync(new FunctionPermissionUpdateRequest
            {
                FunctionPermissionId = rolePermission.FunctionPermissionId,
                FunctionCode = "USER",
                FunctionName = "User",
                OperationCode = "R",
                SortOrder = 10,
                IsEnable = true
            }));
    }

    [TestMethod]
    public async Task DeleteAsync_Should_RemoveDescendants_AndRoleGroupMappings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var roleGroup = CreateRoleGroup("系統管理員");
        db.Sys_RoleGroups.Add(roleGroup);
        var root = CreateFunctionPermission("USER", "使用者管理");
        db.Sys_FunctionPermissions.Add(root);
        await db.SaveChangesAsync();

        var child = CreateFunctionPermission("USER", "使用者管理", "R", root.FunctionPermissionId);
        db.Sys_FunctionPermissions.Add(child);
        await db.SaveChangesAsync();

        db.Sys_RoleGroupFunctionPermissions.Add(new Sys_RoleGroupFunctionPermission
        {
            RoleGroupId = roleGroup.RoleGroupId,
            FunctionPermissionId = child.FunctionPermissionId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var ok = await sut.DeleteAsync(root.FunctionPermissionId);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_FunctionPermissions.AnyAsync());
        Assert.IsFalse(await db.Sys_RoleGroupFunctionPermissions.AnyAsync());

        var logService = scope.ServiceProvider.GetRequiredService<RecordingLogService>();
        Assert.AreEqual(1, logService.UserOperations.Count);
        Assert.AreEqual(AuditActionEnum.Delete, logService.UserOperations[0].Action);
    }

    [TestMethod]
    public async Task SyncFromMenuTreeAsync_Should_CreateFunctionRoot_AndCrudaOperations()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sys_MenuTrees.Add(CreateMenu("USER", "使用者管理"));
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var result = await sut.SyncFromMenuTreeAsync();

        Assert.AreEqual(1, result.CreatedFunctionCount);
        Assert.AreEqual(6, result.CreatedOperationCount);

        var tree = await sut.GetTreeAsync(true);
        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual("USER", tree[0].PermissionKey);
        CollectionAssert.AreEqual(
            new[] { "USER:C", "USER:R", "USER:U", "USER:D", "USER:A", "USER:F" },
            tree[0].Children.Select(x => x.PermissionKey).ToArray());
    }

    [TestMethod]
    public async Task SyncFromMenuTreeAsync_Should_SkipDisabledMenus_ByDefault()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sys_MenuTrees.Add(CreateMenu("ENABLED", "Enabled"));
        var disabled = CreateMenu("DISABLED", "Disabled");
        disabled.IsEnable = false;
        db.Sys_MenuTrees.Add(disabled);
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var result = await sut.SyncFromMenuTreeAsync();

        Assert.AreEqual(1, result.CreatedFunctionCount);
        Assert.AreEqual(6, result.CreatedOperationCount);
        Assert.IsTrue(await db.Sys_FunctionPermissions.AnyAsync(x => x.PermissionKey == "ENABLED"));
        Assert.IsFalse(await db.Sys_FunctionPermissions.AnyAsync(x => x.PermissionKey == "DISABLED"));
    }

    [TestMethod]
    public async Task SyncFromMenuTreeAsync_SecondRun_Should_NotDuplicatePermissions()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();
        db.Sys_MenuTrees.Add(CreateMenu("USER", "User"));
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        _ = await sut.SyncFromMenuTreeAsync();
        var second = await sut.SyncFromMenuTreeAsync();

        Assert.AreEqual(0, second.CreatedFunctionCount);
        Assert.AreEqual(0, second.CreatedOperationCount);
        Assert.AreEqual(1, second.ExistingFunctionCount);
        Assert.AreEqual(6, second.ExistingOperationCount);
        Assert.AreEqual(7, await db.Sys_FunctionPermissions.CountAsync());
    }

    [TestMethod]
    public async Task SyncFromMenuTreeAsync_Should_KeepMenuHierarchy()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var system = CreateMenu("SYSTEM", "系統管理");
        db.Sys_MenuTrees.Add(system);
        await db.SaveChangesAsync();

        db.Sys_MenuTrees.Add(CreateMenu("SYSTEM_USER", "使用者管理", system.Id));
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        await sut.SyncFromMenuTreeAsync();

        var tree = await sut.GetTreeAsync(true);
        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual("SYSTEM", tree[0].PermissionKey);

        var childFunction = tree[0].Children.FirstOrDefault(x => x.PermissionKey == "SYSTEM_USER");
        Assert.IsNotNull(childFunction);
        Assert.AreEqual(6, childFunction.Children.Count);
        Assert.AreEqual("SYSTEM_USER:C", childFunction.Children[0].PermissionKey);
    }

    [TestMethod]
    public async Task UpdateRoleGroupPermissionsAsync_Should_ReplaceMappings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var roleGroup = CreateRoleGroup("系統管理員");
        var read = CreateFunctionPermission("USER", "使用者管理", "R");
        var update = CreateFunctionPermission("USER", "使用者管理", "U");
        db.Sys_RoleGroups.Add(roleGroup);
        db.Sys_FunctionPermissions.AddRange(read, update);
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var ok = await sut.UpdateRoleGroupPermissionsAsync(new RoleGroupFunctionPermissionUpdateRequest
        {
            RoleGroupId = roleGroup.RoleGroupId,
            FunctionPermissionIds = [read.FunctionPermissionId, update.FunctionPermissionId]
        });

        Assert.IsTrue(ok);
        var permissions = await sut.GetRoleGroupPermissionsAsync(roleGroup.RoleGroupId, true);
        Assert.AreEqual(2, permissions.Count);
        CollectionAssert.AreEqual(
            new[] { "USER:R", "USER:U" },
            permissions.Select(x => x.PermissionKey).OrderBy(x => x).ToArray());
    }

    [TestMethod]
    public async Task UpdateRoleGroupPermissionsAsync_EmptyIds_Should_ClearMappings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var roleGroup = CreateRoleGroup("Admin");
        var read = CreateFunctionPermission("USER", "User", "R");
        db.Sys_RoleGroups.Add(roleGroup);
        db.Sys_FunctionPermissions.Add(read);
        await db.SaveChangesAsync();

        db.Sys_RoleGroupFunctionPermissions.Add(new Sys_RoleGroupFunctionPermission
        {
            RoleGroupId = roleGroup.RoleGroupId,
            FunctionPermissionId = read.FunctionPermissionId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var ok = await sut.UpdateRoleGroupPermissionsAsync(new RoleGroupFunctionPermissionUpdateRequest
        {
            RoleGroupId = roleGroup.RoleGroupId,
            FunctionPermissionIds = []
        });

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_RoleGroupFunctionPermissions.AnyAsync());
    }

    [TestMethod]
    public async Task GetRoleGroupPermissionTreeAsync_Should_IncludeAncestorNodes()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var roleGroup = CreateRoleGroup("系統管理員");
        var root = CreateFunctionPermission("USER", "使用者管理");
        db.Sys_RoleGroups.Add(roleGroup);
        db.Sys_FunctionPermissions.Add(root);
        await db.SaveChangesAsync();

        var read = CreateFunctionPermission("USER", "使用者管理", "R", root.FunctionPermissionId);
        db.Sys_FunctionPermissions.Add(read);
        await db.SaveChangesAsync();

        db.Sys_RoleGroupFunctionPermissions.Add(new Sys_RoleGroupFunctionPermission
        {
            RoleGroupId = roleGroup.RoleGroupId,
            FunctionPermissionId = read.FunctionPermissionId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var tree = await sut.GetRoleGroupPermissionTreeAsync(roleGroup.RoleGroupId, true);

        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual("USER", tree[0].PermissionKey);
        Assert.AreEqual(1, tree[0].Children.Count);
        Assert.AreEqual("USER:R", tree[0].Children[0].PermissionKey);
    }

    [TestMethod]
    public async Task GetUserPermissionTreeAsync_Should_ReturnPermissions_FromUserRoleGroups()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        db.Sys_UserInfos.Add(CreateUser("alice"));
        var roleGroup = CreateRoleGroup("系統管理員");
        var root = CreateFunctionPermission("USER", "使用者管理");
        db.Sys_RoleGroups.Add(roleGroup);
        db.Sys_FunctionPermissions.Add(root);
        await db.SaveChangesAsync();

        var read = CreateFunctionPermission("USER", "使用者管理", "R", root.FunctionPermissionId);
        db.Sys_FunctionPermissions.Add(read);
        await db.SaveChangesAsync();

        db.Sys_UserRoleGroups.Add(new Sys_UserRoleGroup
        {
            UserId = "alice",
            RoleGroupId = roleGroup.RoleGroupId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        db.Sys_RoleGroupFunctionPermissions.Add(new Sys_RoleGroupFunctionPermission
        {
            RoleGroupId = roleGroup.RoleGroupId,
            FunctionPermissionId = read.FunctionPermissionId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var tree = await sut.GetUserPermissionTreeAsync("alice", true);

        Assert.AreEqual(1, tree.Count);
        Assert.AreEqual("USER", tree[0].PermissionKey);
        Assert.AreEqual("USER:R", tree[0].Children[0].PermissionKey);
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"function-permission-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();
        services.AddSingleton<RecordingLogService>();
        services.AddScoped<ILogService>(sp => sp.GetRequiredService<RecordingLogService>());

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sys_FunctionPermission CreateFunctionPermission(
        string functionCode,
        string functionName,
        string? operationCode = null,
        int? parentFunctionPermissionId = null,
        int sortOrder = 10)
    {
        var operationName = operationCode switch
        {
            "C" => "新增",
            "R" => "讀取",
            "U" => "更新",
            "D" => "刪除",
            "A" => "審核",
            "F" => "檔案上傳/下載",
            _ => string.Empty
        };

        return new Sys_FunctionPermission
        {
            ParentFunctionPermissionId = parentFunctionPermissionId,
            PermissionKey = string.IsNullOrWhiteSpace(operationCode) ? functionCode : $"{functionCode}:{operationCode}",
            FunctionCode = functionCode,
            FunctionName = functionName,
            OperationCode = operationCode,
            OperationName = operationName,
            SortOrder = sortOrder,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
    }

    private static Sys_MenuTree CreateMenu(string menuCode, string menuName, int? parentId = null)
    {
        return new Sys_MenuTree
        {
            ParentId = parentId,
            MenuCode = menuCode,
            MenuName = menuName,
            Icon = string.Empty,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
    }

    private static Sys_RoleGroup CreateRoleGroup(string roleGroupName)
    {
        return new Sys_RoleGroup
        {
            RoleGroupName = roleGroupName,
            Description = string.Empty,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
    }

    private static Sys_UserInfo CreateUser(string userId)
    {
        return new Sys_UserInfo
        {
            UserId = userId,
            UserName = userId,
            Password = "HASH::password",
            DeptId = 1,
            MobilePhone = "0911222333",
            Email = $"{userId}@example.com",
            IsEnable = true,
            LoginFailCount = 0,
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
