using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Template.WebApi.Services;

namespace Template.Test.Tests;

[TestClass]
public class CurrentUserServiceTests
{
    [TestMethod]
    public void CurrentUser_Unauthenticated_Should_ReturnEmptyModel()
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity())
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });
        var currentUser = service.CurrentUser;

        Assert.AreEqual(string.Empty, currentUser.UserId);
        Assert.AreEqual(string.Empty, currentUser.Email);
        Assert.AreEqual(string.Empty, currentUser.MobilePhone);
        Assert.AreEqual(string.Empty, currentUser.DeptId);
        Assert.AreEqual(string.Empty, currentUser.Ip);
        Assert.AreEqual(0L, currentUser.IssuedTime);
        Assert.AreEqual(0L, currentUser.ExpiredTime);
        Assert.AreEqual(string.Empty, currentUser.TokenId);
    }

    [TestMethod]
    public void CurrentUser_AuthenticatedWithClaims_Should_ParseCorrectly()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "u001"),
            new Claim(ClaimTypes.Email, "u001@example.com"),
            new Claim(ClaimTypes.MobilePhone, "0911222333"),
            new Claim("dept_id", "D01"),
            new Claim("ip", "10.0.0.1"),
            new Claim(JwtRegisteredClaimNames.Iat, "1700000000"),
            new Claim(JwtRegisteredClaimNames.Exp, "1700003600"),
            new Claim(JwtRegisteredClaimNames.Jti, "token-abc")
        ], "test-auth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });
        var currentUser = service.CurrentUser;

        Assert.AreEqual("u001", currentUser.UserId);
        Assert.AreEqual("u001@example.com", currentUser.Email);
        Assert.AreEqual("0911222333", currentUser.MobilePhone);
        Assert.AreEqual("D01", currentUser.DeptId);
        Assert.AreEqual("10.0.0.1", currentUser.Ip);
        Assert.AreEqual(1700000000L, currentUser.IssuedTime);
        Assert.AreEqual(1700003600L, currentUser.ExpiredTime);
        Assert.AreEqual("token-abc", currentUser.TokenId);
    }

    [TestMethod]
    public void CurrentUser_InvalidIatExp_Should_FallbackToZero()
    {
        var identity = new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "u002"),
            new Claim(JwtRegisteredClaimNames.Iat, "not-a-number"),
            new Claim(JwtRegisteredClaimNames.Exp, "invalid")
        ], "test-auth");

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        var service = new CurrentUserService(new HttpContextAccessor { HttpContext = httpContext });
        var currentUser = service.CurrentUser;

        Assert.AreEqual("u002", currentUser.UserId);
        Assert.AreEqual(0L, currentUser.IssuedTime);
        Assert.AreEqual(0L, currentUser.ExpiredTime);
    }
}
