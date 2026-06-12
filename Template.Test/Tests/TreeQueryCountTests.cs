using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.BusinessRule.DepartmentService.Models;
using Template.BusinessRule.DepartmentService.Services;
using Template.BusinessRule.FunctionPermissionService.Models;
using Template.BusinessRule.FunctionPermissionService.Services;
using Template.BusinessRule.MenuTreeService.Models;
using Template.BusinessRule.MenuTreeService.Services;
using Template.BusinessRule.RoleGroupService.Models;
using Template.BusinessRule.RoleGroupService.Services;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;

namespace Template.Test.Tests;

[TestClass]
public class TreeQueryCountTests
{
    [TestMethod]
    public async Task DepartmentTree_Should_Not_Execute_Count_Query()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var scope = BuildSqliteScope(connection, out var interceptor);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        await SeedDepartmentTreeAsync(db);
        interceptor.Reset();

        var sut = new DepartmentService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(0, interceptor.CountQueries);
        Assert.AreEqual(1, interceptor.CountStatements);
        Assert.AreEqual("ROOT", tree[0].DeptName);
    }

    [TestMethod]
    public async Task MenuTree_Should_Not_Execute_Count_Query()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var scope = BuildSqliteScope(connection, out var interceptor);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        await SeedMenuTreeAsync(db);
        interceptor.Reset();

        var sut = new MenuTreeService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(0, interceptor.CountQueries);
        Assert.AreEqual(1, interceptor.CountStatements);
        Assert.AreEqual("ROOT", tree[0].MenuCode);
    }

    [TestMethod]
    public async Task RoleGroupTree_Should_Not_Execute_Count_Query()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var scope = BuildSqliteScope(connection, out var interceptor);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        await SeedRoleGroupTreeAsync(db);
        interceptor.Reset();

        var sut = new RoleGroupService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(0, interceptor.CountQueries);
        Assert.AreEqual(1, interceptor.CountStatements);
        Assert.AreEqual("ROOT", tree[0].RoleGroupName);
    }

    [TestMethod]
    public async Task FunctionPermissionTree_Should_Not_Execute_Count_Query()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        using var scope = BuildSqliteScope(connection, out var interceptor);
        var db = scope.ServiceProvider.GetRequiredService<ProjectDbContext>();

        await SeedFunctionPermissionTreeAsync(db);
        interceptor.Reset();

        var sut = new FunctionPermissionService(scope.ServiceProvider);
        var tree = await sut.GetTreeAsync(true);

        Assert.AreEqual(0, interceptor.CountQueries);
        Assert.AreEqual(1, interceptor.CountStatements);
        Assert.AreEqual("ROOT", tree[0].PermissionKey);
    }

    private static IServiceScope BuildSqliteScope(SqliteConnection connection, out CountingDbCommandInterceptor interceptor)
    {
        var services = new ServiceCollection();
        interceptor = new CountingDbCommandInterceptor();
        var dbInterceptor = interceptor;

        services.AddSingleton(interceptor);
        services.AddDbContext<ProjectDbContext>(options =>
            options.UseSqlite(connection).AddInterceptors(dbInterceptor));
        services.AddScoped<ICurrentUserService, FakeCurrentUserService>();

        var provider = services.BuildServiceProvider();
        var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ProjectDbContext>().Database.EnsureCreated();
        return scope;
    }

    private static async Task SeedDepartmentTreeAsync(ProjectDbContext db)
    {
        var root = new Sys_Department
        {
            DeptName = "ROOT",
            ParentDeptId = null,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
        db.Sys_Departments.Add(root);
        await db.SaveChangesAsync();

        db.Sys_Departments.Add(new Sys_Department
        {
            DeptName = "CHILD",
            ParentDeptId = root.DeptId,
            SortOrder = 20,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedMenuTreeAsync(ProjectDbContext db)
    {
        var root = new Sys_MenuTree
        {
            ParentId = null,
            MenuCode = "ROOT",
            MenuName = "Root",
            Icon = string.Empty,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
        db.Sys_MenuTrees.Add(root);
        await db.SaveChangesAsync();

        db.Sys_MenuTrees.Add(new Sys_MenuTree
        {
            ParentId = root.Id,
            MenuCode = "CHILD",
            MenuName = "Child",
            Icon = string.Empty,
            SortOrder = 20,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedRoleGroupTreeAsync(ProjectDbContext db)
    {
        var root = new Sys_RoleGroup
        {
            ParentRoleGroupId = null,
            RoleGroupName = "ROOT",
            Description = string.Empty,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
        db.Sys_RoleGroups.Add(root);
        await db.SaveChangesAsync();

        db.Sys_RoleGroups.Add(new Sys_RoleGroup
        {
            ParentRoleGroupId = root.RoleGroupId,
            RoleGroupName = "CHILD",
            Description = string.Empty,
            SortOrder = 20,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedFunctionPermissionTreeAsync(ProjectDbContext db)
    {
        var root = new Sys_FunctionPermission
        {
            ParentFunctionPermissionId = null,
            PermissionKey = "ROOT",
            FunctionCode = "ROOT",
            FunctionName = "Root",
            OperationCode = null,
            OperationName = string.Empty,
            SortOrder = 10,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        };
        db.Sys_FunctionPermissions.Add(root);
        await db.SaveChangesAsync();

        db.Sys_FunctionPermissions.Add(new Sys_FunctionPermission
        {
            ParentFunctionPermissionId = root.FunctionPermissionId,
            PermissionKey = "ROOT:R",
            FunctionCode = "ROOT",
            FunctionName = "Root",
            OperationCode = "R",
            OperationName = "查詢",
            SortOrder = 20,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });
        await db.SaveChangesAsync();
    }

    private sealed class CountingDbCommandInterceptor : DbCommandInterceptor
    {
        public int CountQueries { get; private set; }
        public int CountStatements { get; private set; }

        public void Reset()
        {
            CountQueries = 0;
            CountStatements = 0;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            CountCommand(command.CommandText);
            return base.ReaderExecuting(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            CountCommand(command.CommandText);
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        private void CountCommand(string commandText)
        {
            if (commandText.Contains("COUNT(", StringComparison.OrdinalIgnoreCase))
                CountQueries++;
            else
                CountStatements++;
        }
    }

    private sealed class FakeCurrentUserService : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new CurrentUser { UserId = "tester", Email = "tester@localhost" };
    }
}
