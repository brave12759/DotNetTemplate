using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.Common.Models;
using Template.Common.Services;
using Template.DataAccess.ProjectDbContext;
using Template.WebApi.Filters;

namespace Template.Test.Tests;

[TestClass]
public class GlobalExceptionLogFilterTests
{
    [TestMethod]
    public void Order_Should_BeIntMinValue()
    {
        var filter = new GlobalExceptionLogFilter(NullLogger<GlobalExceptionLogFilter>.Instance);
        Assert.AreEqual(int.MinValue, filter.Order);
    }

    [TestMethod]
    public void OnException_Should_Set500Result_AndHandled()
    {
        var filter = new GlobalExceptionLogFilter(NullLogger<GlobalExceptionLogFilter>.Instance);
        var httpContext = new DefaultHttpContext();
        httpContext.TraceIdentifier = "trace-001";
        httpContext.Request.Method = HttpMethods.Post;
        httpContext.Request.Path = "/api/test";
        httpContext.Request.QueryString = new QueryString("?id=1");
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "tester"),
            new Claim("jti", "token-123")
        ], "test"));

        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var exceptionContext = new ExceptionContext(actionContext, [])
        {
            Exception = new InvalidOperationException("boom")
        };

        filter.OnException(exceptionContext);

        Assert.IsTrue(exceptionContext.ExceptionHandled);
        Assert.IsInstanceOfType<ObjectResult>(exceptionContext.Result);

        var result = (ObjectResult)exceptionContext.Result;
        Assert.AreEqual(500, result.StatusCode);

        var payload = result.Value as ResponseMessage<object>;
        Assert.IsNotNull(payload);
        Assert.AreEqual(500, payload.Status);
        Assert.AreEqual("系統發生未預期錯誤，請稍後再試。", payload.Message);
    }
}

[TestClass]
public class RequirePermissionFilterTests
{
    [TestMethod]
    public async Task OnAuthorizationAsync_EmptyPermissionKeys_Should_Skip()
    {
        var db = CreateDb();
        var filter = new RequirePermissionFilter([], db, new FixedCurrentUserService("tester"));
        var context = CreateAuthorizationContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public async Task OnAuthorizationAsync_EmptyUserId_Should_Forbid()
    {
        var db = CreateDb();
        var filter = new RequirePermissionFilter(["USER:READ"], db, new FixedCurrentUserService(string.Empty));
        var context = CreateAuthorizationContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsInstanceOfType<ForbidResult>(context.Result);
    }

    [TestMethod]
    public async Task OnAuthorizationAsync_UserHasPermission_Should_Allow()
    {
        var db = CreateDb();
        SeedPermissionGraph(db, userId: "tester", permissionKey: "USER:READ", isEnable: true);

        var filter = new RequirePermissionFilter(["USER:READ"], db, new FixedCurrentUserService("tester"));
        var context = CreateAuthorizationContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsNull(context.Result);
    }

    [TestMethod]
    public async Task OnAuthorizationAsync_PermissionDisabled_Should_Forbid()
    {
        var db = CreateDb();
        SeedPermissionGraph(db, userId: "tester", permissionKey: "USER:READ", isEnable: false);

        var filter = new RequirePermissionFilter(["USER:READ"], db, new FixedCurrentUserService("tester"));
        var context = CreateAuthorizationContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsInstanceOfType<ForbidResult>(context.Result);
    }

    [TestMethod]
    public async Task OnAuthorizationAsync_UserLacksPermission_Should_Forbid()
    {
        var db = CreateDb();
        SeedPermissionGraph(db, userId: "tester", permissionKey: "USER:WRITE", isEnable: true);

        var filter = new RequirePermissionFilter(["USER:READ"], db, new FixedCurrentUserService("tester"));
        var context = CreateAuthorizationContext();

        await filter.OnAuthorizationAsync(context);

        Assert.IsInstanceOfType<ForbidResult>(context.Result);
    }

    private static ProjectDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ProjectDbContext>()
            .UseInMemoryDatabase($"require-permission-tests-{Guid.NewGuid()}")
            .Options;

        return new ProjectDbContext(options);
    }

    private static AuthorizationFilterContext CreateAuthorizationContext()
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new AuthorizationFilterContext(actionContext, []);
    }

    private static void SeedPermissionGraph(ProjectDbContext db, string userId, string permissionKey, bool isEnable)
    {
        db.Sys_RoleGroups.Add(new Sys_RoleGroup
        {
            RoleGroupId = 101,
            RoleGroupName = "RG-101",
            Description = "seed-role-group",
            SortOrder = 0,
            IsEnable = true,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });

        db.Sys_UserRoleGroups.Add(new Sys_UserRoleGroup
        {
            UserId = userId,
            RoleGroupId = 101,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });

        db.Sys_FunctionPermissions.Add(new Sys_FunctionPermission
        {
            FunctionPermissionId = 501,
            PermissionKey = permissionKey,
            FunctionCode = "USER",
            FunctionName = "User",
            OperationCode = "R",
            OperationName = "Read",
            SortOrder = 0,
            IsEnable = isEnable,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed",
            UpdatedTime = DateTime.UtcNow,
            UpdatedId = "seed"
        });

        db.Sys_RoleGroupFunctionPermissions.Add(new Sys_RoleGroupFunctionPermission
        {
            RoleGroupId = 101,
            FunctionPermissionId = 501,
            CreatedTime = DateTime.UtcNow,
            CreatedId = "seed"
        });

        db.SaveChanges();
    }

    private sealed class FixedCurrentUserService(string userId) : ICurrentUserService
    {
        public CurrentUser CurrentUser { get; } = new() { UserId = userId };
    }
}
