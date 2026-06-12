using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.RoleGroupService.Models;
using Template.BusinessRule.RoleGroupService.Services;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class RoleGroupServiceTests
{
    [TestMethod]
    public async Task CreateAsync_Should_CreateRoleGroup_AndSetAuditFields()
    {
        using var scope = BuildScope();
        var sut = new RoleGroupService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new RoleGroupCreateRequest
        {
            RoleGroupName = "系統管理員",
            Description = "系統管理權限",
            SortOrder = 10,
            IsEnable = true
        });

        Assert.IsTrue(created.RoleGroupId > 0);
        Assert.AreEqual("系統管理員", created.RoleGroupName);
        Assert.AreEqual("tester", created.CreatedId);
        Assert.AreEqual("tester", created.UpdatedId);
    }

    [TestMethod]
    public async Task CreateAsync_NullDescription_Should_SaveEmptyString()
    {
        using var scope = BuildScope();
        var sut = new RoleGroupService(scope.ServiceProvider);

        var created = await sut.CreateAsync(new RoleGroupCreateRequest
        {
            RoleGroupName = "一般使用者",
            Description = null!
        });

        Assert.AreEqual(string.Empty, created.Description);
    }

    [TestMethod]
    public async Task GetTreeAsync_Should_ReturnParentChildHierarchy()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateRoleGroup("系統管理員", null, 20);
        var otherRoot = CreateRoleGroup("一般使用者", null, 10);
        db.Sys_RoleGroups.AddRange(root, otherRoot);
        await db.SaveChangesAsync();

        db.Sys_RoleGroups.AddRange(
            CreateRoleGroup("使用者管理", root.RoleGroupId, 20),
            CreateRoleGroup("選單管理", root.RoleGroupId, 10));
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(2, tree.Count);
        Assert.AreEqual("一般使用者", tree[0].RoleGroupName);
        Assert.AreEqual("系統管理員", tree[1].RoleGroupName);
        Assert.AreEqual(2, tree[1].Children.Count);
        Assert.AreEqual("選單管理", tree[1].Children[0].RoleGroupName);
        Assert.AreEqual("使用者管理", tree[1].Children[1].RoleGroupName);
    }

    [TestMethod]
    public async Task GetByIdAsync_Should_ReturnRoleGroup_ByRoleGroupId()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var roleGroup = CreateRoleGroup("系統管理員");
        db.Sys_RoleGroups.Add(roleGroup);
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var found = await sut.GetByIdAsync(roleGroup.RoleGroupId);

        Assert.IsNotNull(found);
        Assert.AreEqual(roleGroup.RoleGroupId, found.RoleGroupId);
        Assert.AreEqual("系統管理員", found.RoleGroupName);
    }

    [TestMethod]
    public async Task UpdateAsync_Should_UpdateRoleGroup_ByRoleGroupId()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var roleGroup = CreateRoleGroup("系統管理員");
        db.Sys_RoleGroups.Add(roleGroup);
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var ok = await sut.UpdateAsync(new RoleGroupUpdateRequest
        {
            RoleGroupId = roleGroup.RoleGroupId,
            RoleGroupName = "系統管理員更新",
            Description = "更新後描述",
            SortOrder = 30,
            IsEnable = false
        });

        Assert.IsTrue(ok);

        var updated = await db.Sys_RoleGroups.FirstAsync(x => x.RoleGroupId == roleGroup.RoleGroupId);
        Assert.AreEqual("系統管理員更新", updated.RoleGroupName);
        Assert.AreEqual("更新後描述", updated.Description);
        Assert.AreEqual(30, updated.SortOrder);
        Assert.IsFalse(updated.IsEnable);
        Assert.AreEqual("tester", updated.UpdatedId);
    }

    [TestMethod]
    public async Task UpdateAsync_ParentIsDescendant_Should_Throw()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        var root = CreateRoleGroup("系統管理員");
        db.Sys_RoleGroups.Add(root);
        await db.SaveChangesAsync();

        var child = CreateRoleGroup("使用者管理", root.RoleGroupId);
        db.Sys_RoleGroups.Add(child);
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);

        await Assert.ThrowsExceptionAsync<ArgumentException>(() =>
            sut.UpdateAsync(new RoleGroupUpdateRequest
            {
                RoleGroupId = root.RoleGroupId,
                ParentRoleGroupId = child.RoleGroupId,
                RoleGroupName = "系統管理員",
                IsEnable = true
            }));
    }

    [TestMethod]
    public async Task DeleteAsync_Should_RemoveDescendants_AndUserMappings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        db.Sys_UserInfos.Add(CreateUser("alice"));

        var root = CreateRoleGroup("系統管理員");
        db.Sys_RoleGroups.Add(root);
        await db.SaveChangesAsync();

        var child = CreateRoleGroup("使用者管理", root.RoleGroupId);
        db.Sys_RoleGroups.Add(child);
        await db.SaveChangesAsync();

        var grandChild = CreateRoleGroup("使用者編輯", child.RoleGroupId);
        db.Sys_RoleGroups.Add(grandChild);
        await db.SaveChangesAsync();

        db.Sys_UserRoleGroups.Add(new Sys_UserRoleGroup
        {
            UserId = "alice",
            RoleGroupId = grandChild.RoleGroupId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var ok = await sut.DeleteAsync(root.RoleGroupId);

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_RoleGroups.AnyAsync());
        Assert.IsFalse(await db.Sys_UserRoleGroups.AnyAsync());
    }

    [TestMethod]
    public async Task UpdateUserRoleGroupsAsync_Should_ReplaceMappings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        db.Sys_UserInfos.Add(CreateUser("alice"));
        var admin = CreateRoleGroup("系統管理員", null, 10);
        var user = CreateRoleGroup("一般使用者", null, 20);
        db.Sys_RoleGroups.AddRange(admin, user);
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var ok = await sut.UpdateUserRoleGroupsAsync(new UserRoleGroupUpdateRequest
        {
            UserId = "alice",
            RoleGroupIds = [admin.RoleGroupId, user.RoleGroupId]
        });

        Assert.IsTrue(ok);
        var roleGroups = await sut.GetUserRoleGroupsAsync("alice", true);
        Assert.AreEqual(2, roleGroups.Count);
        Assert.AreEqual(admin.RoleGroupId, roleGroups[0].RoleGroupId);
        Assert.AreEqual(user.RoleGroupId, roleGroups[1].RoleGroupId);
    }

    [TestMethod]
    public async Task UpdateUserRoleGroupsAsync_EmptyRoleGroupIds_Should_ClearMappings()
    {
        using var scope = BuildScope();
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        db.Sys_UserInfos.Add(CreateUser("alice"));
        var admin = CreateRoleGroup("系統管理員");
        db.Sys_RoleGroups.Add(admin);
        await db.SaveChangesAsync();

        db.Sys_UserRoleGroups.Add(new Sys_UserRoleGroup
        {
            UserId = "alice",
            RoleGroupId = admin.RoleGroupId,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });
        await db.SaveChangesAsync();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var ok = await sut.UpdateUserRoleGroupsAsync(new UserRoleGroupUpdateRequest
        {
            UserId = "alice",
            RoleGroupIds = []
        });

        Assert.IsTrue(ok);
        Assert.IsFalse(await db.Sys_UserRoleGroups.AnyAsync());
    }

    private static IServiceScope BuildScope()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ProjectDbContext>(options =>
            options.UseInMemoryDatabase($"role-group-service-tests-{Guid.NewGuid()}"));

        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

        var provider = services.BuildServiceProvider();
        return provider.CreateScope();
    }

    private static Sys_RoleGroup CreateRoleGroup(
        string roleGroupName,
        int? parentId = null,
        int sortOrder = 10)
    {
        return new Sys_RoleGroup
        {
            ParentRoleGroupId = parentId,
            RoleGroupName = roleGroupName,
            Description = string.Empty,
            SortOrder = sortOrder,
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
}
